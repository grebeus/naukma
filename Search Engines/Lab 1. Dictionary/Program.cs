using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.RegularExpressions;

namespace Dictionary
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime startTime = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Process started.");

            string folderPath = GetFolderPath(args); //Folder path is the first argument
            string dictionaryFile = GetAndDeleteDictionaryFile(args, folderPath); //Dictionary file is the secund argument
            
            List<string> tokens = new List<string>();

            int wordsCount = 0;
            foreach (string filePath in Directory.GetFiles(folderPath))
                wordsCount += TokenizeFileAndGetWordsCount(filePath, ref tokens);
            
            SaveDictionaryFile(ref tokens, dictionaryFile);

            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Process completed in " + Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2) + " seconds.");

            Console.WriteLine();
            Console.WriteLine("Collection size (words count): " + wordsCount + ".");
            Console.WriteLine("Dictionary size (tokens count): " + tokens.Count + ".");
            Console.WriteLine();
            Console.WriteLine("Bingo!");
            Console.ReadKey();
        }

        static string GetFolderPath(string[] args)
        {
            if (args.Length > 0)
                return args[0];
            else
                return "c:\\Test\\NaUKMA\\Books\\1";
        }

        static string GetAndDeleteDictionaryFile(string[] args, string folderPath)
        {
            string dictionaryFile = Path.Join(folderPath, "Dictionary.txt");
            if (args.Length == 2)
            {
                dictionaryFile = args[1];
            }
            if (File.Exists(dictionaryFile))
                File.Delete(dictionaryFile);

            return dictionaryFile;
        }

        static int TokenizeFileAndGetWordsCount(string filePath, ref List<string> tokens)
        {
            {
                Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Processing file: " + filePath);

                string[] words = Regex.Split(File.ReadAllText(filePath), @"\W");

                foreach (string word in words)
                    if (!(String.IsNullOrWhiteSpace(word) || String.IsNullOrEmpty(word)))
                        if (!tokens.Contains(word.ToLower()))
                            tokens.Add(word.ToLower());
                
                return words.Length;
            }
        }

        static void SaveDictionaryFile(ref List<string> tokens, string dictionaryFile)
        {
            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Saving dictionary: " + dictionaryFile);

            using (TextWriter textWriter = new StreamWriter(dictionaryFile))
            {
                foreach (string token in tokens)
                    textWriter.WriteLine(token);
            }
        }

    }
}
