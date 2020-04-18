using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace Dictionary
{
    class Program
    {
        public static string collectionFolder = "F:\\NaUKMA\\Semester 1.2\\Information Retrieval\\Collection (test)";
        public static string systemFolder = collectionFolder + "\\.system";

        public static Dictionary<int, string> fileCollection = new Dictionary<int, string>();
        public static int collectionSize = 0;

        public static SortedSet<string> dictionary = new SortedSet<string>();
        public static int wordsCount = 0;

        public static SortedDictionary<string, bool[]> incidenceMatrix = new SortedDictionary<string, bool[]>();
        public static SortedDictionary<string, List<int>> invertedIndex = new SortedDictionary<string, List<int>>();

        public static readonly string searchInstruction =
            "\n\nType your search request (single words; AND, OR, NOT operators; no grouping; case insensitive; EXIT to leave):\n>>> ";

        static void Main(string[] args)
        {
            //Console.InputEncoding = Encoding.Default;
            Console.OutputEncoding = Encoding.Default;

            SetCollectionFolder(args);
            ClearSystemFiles();

            TaxonomizeCollection();
            IndexCollection();

            Console.Write(searchInstruction);
            string request = Console.ReadLine();

            while (!request.Trim().ToLower().Equals("exit"))
            {
                IncidenceMatrixSearch(request);
                InvertedIndexSearch(request);

                Console.Write(searchInstruction);
                request = Console.ReadLine();
            }
        }

        // Indexing
        private static void SetCollectionFolder(string[] args)
        {
            if (args.Length > 0)
                collectionFolder = args[0].TrimEnd('\\');
        }

        static void ClearSystemFiles()
        {
            if (Directory.Exists(systemFolder))
                Directory.Delete(systemFolder, true);

            Directory.CreateDirectory(systemFolder);
        }

        private static void TaxonomizeCollection()
        {
            int fileNumber = 0;
            foreach (string filePath in Directory.GetFiles(collectionFolder))
            {
                fileCollection.Add(fileNumber, filePath);
                fileNumber++;
            }
            collectionSize = fileCollection.Count;
        }

        private static void IndexCollection()
        {
            CreateDictionary();
            CreateIncedenceMatrix();
            CreateInvertedIndex();
        }

        private static void CreateDictionary()
        {
            DateTime startTime = DateTime.UtcNow;
            foreach (var filePath in fileCollection.Values)
            {
                string[] words = SplitFileContentByWords(filePath);
                foreach (string word in words)
                    AddTokenToDictionary(Tokenize(word));
                
                wordsCount += words.Length;
            }

            Console.WriteLine("\nDictionary created in " + Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2) + " seconds:");
            
            string jsonFile = systemFolder + "\\dictionary.json";
            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(dictionary, Formatting.Indented));
            Console.WriteLine("> " + jsonFile + "  " + Math.Round((decimal)(new FileInfo(jsonFile)).Length / 1024) + " KB");

            Console.WriteLine("Collection size (words count): " + wordsCount + ".");
            Console.WriteLine("Dictionary size (tokens count): " + dictionary.Count + ".");
        }
        private static void AddTokenToDictionary(string token)
        {
            if (String.IsNullOrWhiteSpace(token) || String.IsNullOrEmpty(token))
                return;

            if (!dictionary.Contains(token))
                dictionary.Add(token);
        }

        private static void CreateIncedenceMatrix()
        {
            DateTime startTime = DateTime.UtcNow;
            foreach (var file in fileCollection)
                foreach (string word in SplitFileContentByWords(file.Value))
                    AddTokenToIncidenceMatrix(Tokenize(word), file.Key);

            Console.WriteLine("\nIncedence matrix created in " + Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2) + " seconds:");
            
            string jsonFile = systemFolder + "\\incidence_matrix.json";
            SortedDictionary<string, List<byte>> incidenceLists = new SortedDictionary<string, List<byte>>(); //bools converted to 0,1 representation to compare collections by json size
            foreach (KeyValuePair<string, bool[]> incidenceArray in incidenceMatrix)
                incidenceLists.Add(incidenceArray.Key, incidenceArray.Value.Select(x => Convert.ToByte(x)).ToList());
            
            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(incidenceLists, Formatting.Indented));
            Console.WriteLine("> " + jsonFile + "  " + Math.Round((decimal)(new FileInfo(jsonFile)).Length / 1024) + " KB");
        }

        private static void AddTokenToIncidenceMatrix(string token, int fileNumber)
        {
            if (String.IsNullOrWhiteSpace(token) || String.IsNullOrEmpty(token))
                return;

            if (!incidenceMatrix.ContainsKey(token))
                incidenceMatrix.Add(token, new bool[collectionSize]);

            incidenceMatrix[token][fileNumber] = true;
        }

        private static void CreateInvertedIndex()
        {
            DateTime startTime = DateTime.UtcNow;
            foreach (var file in fileCollection)
                foreach (string word in SplitFileContentByWords(file.Value))
                    AddTokenToInvertedIndex(Tokenize(word), file.Key);

            Console.WriteLine("\nInverted index created in " + Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2) + " seconds:");

            string jsonFile = systemFolder + "\\inverted_index.json";
            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(invertedIndex, Formatting.Indented));
            Console.WriteLine("> " + jsonFile + "  " + Math.Round((decimal)(new FileInfo(jsonFile)).Length / 1024) + " KB");
        }

        private static void AddTokenToInvertedIndex(string token, int fileNumber)
        {
            if (String.IsNullOrWhiteSpace(token) || String.IsNullOrEmpty(token))
                return;

            if (!invertedIndex.ContainsKey(token))
                invertedIndex.Add(token, new List<int>());

            if (!invertedIndex[token].Contains(fileNumber))
            {
                invertedIndex[token].Add(fileNumber);
                invertedIndex[token].Sort();
            }
        }



        private static string[] SplitFileContentByWords(string filePath)
        {
            return Regex.Split(File.ReadAllText(filePath, Encoding.Default), @"\W");
        }
        
        private static string Tokenize(string word)
        {
            //return Regex.Replace(word.ToLower(), "[_0-9]", string.Empty);
            return Regex.Replace(word.ToLower(), "[_]", string.Empty);
        }

        private static void AddTokenToIndexes(string token, int fileNumber)
        {
            if (String.IsNullOrWhiteSpace(token) || String.IsNullOrEmpty(token))
                return;

            AddTokenToDictionary(token);
            AddTokenToIncidenceMatrix(token, fileNumber);
            AddTokenToInvertedIndex(token, fileNumber);
        }
        
        





        // Search engine
        private static void IncidenceMatrixSearch(string request)
        {
            DateTime searchStart = DateTime.UtcNow;
            
            bool or = false;
            bool and = false;
            bool not = false;
            bool[] searchResult = new bool[collectionSize];

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
                        bool[] currentMatch = new bool[collectionSize];
                        try
                        {
                            currentMatch = incidenceMatrix[token].Select(x => x).ToArray();
                        }
                        catch
                        { }

                        if (not)
                            currentMatch = currentMatch.Select(x => !x).ToArray();

                        if (!and && !or)
                            searchResult = currentMatch;
                        else if (and)
                            for (int i = 0; i < collectionSize; i++)
                                searchResult[i] = searchResult[i] && currentMatch[i];
                        //searchResult = searchResult.Join(currentMatch, x => x, y => y, (x, y) => x && y).ToArray();
                        //searchResult = searchResult.SelectMany(x => currentMatch, (x, y) => x && y).ToArray();
                        else if (or)
                            //searchResult = searchResult.Join(currentMatch, x => x, y => y, (x, y) => x || y).ToArray();
                            //searchResult = searchResult.SelectMany(x => currentMatch, (x, y) => x || y).ToArray();
                            for (int i = 0; i < collectionSize; i++)
                                searchResult[i] = searchResult[i] || currentMatch[i];

                        or = false;
                        and = false;
                        not = false;
                        break;
                }
            }

            IncidenceMatrixPrintResults(searchResult, Math.Round((DateTime.UtcNow - searchStart).TotalMilliseconds, 2));
        }

        private static void IncidenceMatrixPrintResults(bool[] incidenceArray, double durationMs)
        {
            Console.WriteLine("\nSearch results (incidence matrix), completed in " + durationMs + " ms:");
            for (int fileNumber = 0; fileNumber < collectionSize; fileNumber++)
                if (incidenceArray[fileNumber])
                    Console.WriteLine("> " + fileCollection[fileNumber].ToString());
        }

        private static void InvertedIndexSearch(string request)
        {
            DateTime searchStart = DateTime.UtcNow;

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
                        List<int> currentMatch;
                        try
                        {
                            currentMatch = new List<int>(invertedIndex[token]);
                        }
                        catch
                        {
                            currentMatch = new List<int>();
                        }

                        if (not)
                            currentMatch = fileCollection.Keys.Except(currentMatch).ToList();

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

            InvertedIndexPrintResults(searchResult, Math.Round((DateTime.UtcNow - searchStart).TotalMilliseconds, 2));
        }

        private static void InvertedIndexPrintResults(List<int> fileNumbers, double durationMs)
        {
            Console.WriteLine("\nSearch results (inverted index), completed in " + durationMs + " ms:");
            foreach (int fileNumber in fileNumbers)
                Console.WriteLine("> " + fileCollection[fileNumber].ToString());
        }

        // Managing lists
        private static List<int> InvertedIndexesAnd(IEnumerable<int> set1, IEnumerable<int> set2)
        {
            return set1.Intersect(set2).ToList();
        }

        private static List<int> InvertedIndexesOr(IEnumerable<int> set1, IEnumerable<int> set2)
        {
            return set1.Union(set2).ToList();
        }
        private static List<int> InvertedIndexesNot(IEnumerable<int> set)
        {
            return fileCollection.Keys.Except(set).ToList();
        }

        // Managing bool arrays
        private static bool[] IncidenceMatrixAnd(bool[] array1, bool[] array2)
        {
            return array1.SelectMany(x => array2, (x,y) => x && y).ToArray();
        }

        private static bool[] IncidenceMatrixOr(bool[] array1, bool[] array2)
        {
            return array1.SelectMany(x => array2, (x, y) => x || y).ToArray();
        }
        private static bool[] IncidenceMatrixNot(bool[] array)
        {
            return array.Select(x => !x).ToArray();
        }
    }
}
