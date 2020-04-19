using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

namespace Dictionary
{
    class Program
    {
        public static string collectionFolder = "F:\\NaUKMA\\Semester 1.2\\Information Retrieval\\Collection (test)";
        public static readonly string postgreConnString = "Host=localhost;Username=postgres;Password=sa;Database=naukma";
        public static List<int> allDocuments = new List<int>();
        public static readonly string searchInstruction =
            "\n\nType your search request (single words; AND, OR, NOT operators; no grouping; case insensitive; EXIT to leave):\n>>> ";

        static void Main(string[] args)
        {
            SetCollectionFolder(args);

            ParseCollection();

            Console.Write(searchInstruction);
            Console.OutputEncoding = Encoding.UTF8;
            string request = Console.ReadLine();

            while (!request.Trim().ToLower().Equals("exit"))
            {
                PostgreSqlSearch(request);

                Console.Write(searchInstruction);
                request = Console.ReadLine();
            }
        }

        // Indexing
        private static void SetCollectionFolder(string[] args)
        {
            if (args.Length > 0)
                collectionFolder = args[0].TrimEnd('\\');
            else
                if (!Directory.Exists(collectionFolder))
                {
                    Console.WriteLine("Enter the folder that stores collection or press ENTER to use default: ");
                    collectionFolder = Console.ReadLine().TrimEnd('\\');
                }
        }

        private static void ParseCollection()
        {
            DateTime startTime = DateTime.UtcNow;

            using var con = new NpgsqlConnection(postgreConnString);
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = "DROP TABLE IF EXISTS document";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE document(id SERIAL PRIMARY KEY, document_name VARCHAR(255))";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "DROP TABLE IF EXISTS token_document";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE token_document(id SERIAL PRIMARY KEY, token VARCHAR(255), document_id integer)";
            cmd.ExecuteNonQuery();

            foreach (string filePath in Directory.GetFiles(collectionFolder))
            {
                cmd.CommandText = String.Format(
                    "INSERT INTO document(id, document_name) " +
                    "VALUES(DEFAULT, '{0}') " +
                    "RETURNING id", 
                    filePath
                    );
                int fileNumber = Int32.Parse(cmd.ExecuteScalar().ToString());
                allDocuments.Add(fileNumber);

                foreach (string word in SplitFileContentByWords(filePath))
                {
                    string token = Tokenize(word);
                    if (!(String.IsNullOrWhiteSpace(token) || String.IsNullOrEmpty(token)))
                    {
                        cmd.CommandText = String.Format(
                            "INSERT INTO token_document(id, token, document_id) " +
                            "VALUES(DEFAULT, '{0}', {1}) " +
                            "ON CONFLICT DO NOTHING", 
                            token, fileNumber);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            Console.WriteLine("\nParsing completed in " + Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2) + " seconds.");

        }
        private static string[] SplitFileContentByWords(string filePath)
        {
            return Regex.Split(File.ReadAllText(filePath, Encoding.UTF8), @"\W");
        }

        private static string Tokenize(string word)
        {
            //return Regex.Replace(word.ToLower(), "[_0-9]", string.Empty);
            return Regex.Replace(word.ToLower(), "[_]", string.Empty);
        }

        private static void PostgreSqlSearch(string request)
        {
            DateTime searchStart = DateTime.UtcNow;
            using var con = new NpgsqlConnection(postgreConnString);
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            bool or = false;
            bool and = false;
            bool not = false;
            List<int> searchResult = new List<int>();

            string[] requestParts = request.Split(' ');
            foreach (string requestPart in requestParts)
            {
                string token = Tokenize(requestPart);
                switch (token)
                {
                    case "":
                        break;
                    case "and":
                        and = true;
                        break;
                    case "or":
                        or = true;
                        break;
                    case "not":
                        not = true;
                        break;
                    default:
                        List<int> currentMatch = new List<int>();

                        cmd.CommandText = String.Format(
                            "SELECT document_id " +
                            "FROM token_document " +
                            "WHERE token = '{0}'",
                            token
                            );

                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                                currentMatch.Add(reader.GetInt32(0));

                        if (not)
                            currentMatch = allDocuments.Except(currentMatch).ToList();

                        if (!and && !or)
                            searchResult = currentMatch;
                        else if (and)
                            searchResult = searchResult.Intersect(currentMatch).ToList();
                        else if (or)
                            searchResult = searchResult.Union(currentMatch).ToList();

                        or = false;
                        and = false;
                        not = false;
                        break;
                }
            }

            Console.WriteLine("\nSearch results (inverted index), completed in " + Math.Round((DateTime.UtcNow - searchStart).TotalMilliseconds) + " ms:");

            foreach (int documentId in searchResult)
            {
                cmd.CommandText = String.Format(
                    "SELECT document_name " +
                    "FROM document " +
                    "WHERE id = '{0}'",
                    documentId
                    );
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        Console.WriteLine("> " + reader.GetString(0));
            }
        }
    }
}
