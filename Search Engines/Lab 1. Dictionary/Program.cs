using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Dictionary
{
    class Program
    {
        public static string inputFilesFolder = "c:\\Test\\NaUKMA\\Books\\1";
        public static string outputDictionaryFile = "Dictionary.txt";

        //public static List<string> tokens = new List<string>();
        public static HashSet<string> tokens = new HashSet<string>();
        //public static SortedSet<string> tokens = new SortedSet<string>();
        //public static SortedDictionary<string, List<int>> tokens = new SortedDictionary<string, List<int>>();

        public static int wordsCount = 0;
        static void Main(string[] args)
        {
            DateTime startTime = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Process started.");
            
            ProcessArguments(args);
            GenerateDictionary();

            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Process completed in " + Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2) + " seconds.");
            Console.WriteLine();
            Console.WriteLine("Collection size (words count): " + wordsCount + ".");
            Console.WriteLine("Dictionary size (tokens count): " + tokens.Count + ".");
            Console.WriteLine();
            Console.WriteLine("Bingo!");
            Console.ReadKey();
        }

        private static void ProcessArguments(string[] args)
        {
            SetInputFilesFolder(args); // Input folder path expected to be the first argument
            SetAndDeleteOutputDictionaryFile(args); //Dictionary file is the second argument
        }

        static void SetInputFilesFolder(string[] args)
        {
            if (args.Length > 0)
                inputFilesFolder = args[0];
        }

        static void SetAndDeleteOutputDictionaryFile(string[] args)
        {
            if (args.Length > 1)
                outputDictionaryFile = args[1];

            if (!outputDictionaryFile.Contains('/'))
                outputDictionaryFile = Path.Join(inputFilesFolder, outputDictionaryFile);

            if (File.Exists(outputDictionaryFile))
                File.Delete(outputDictionaryFile);
        }

        private static void GenerateDictionary()
        {
            foreach (string filePath in Directory.GetFiles(inputFilesFolder))
            {
                Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Processing file: " + filePath);
                TokenizeFileAndCountWords(filePath);
            }

            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Saving dictionary: " + outputDictionaryFile);
            SaveDictionaryFile();
        }

        private static void TokenizeFileAndCountWords(string filePath)
        {
            string[] words = SplitFileContentByWords(filePath);

            foreach (string word in words)
                AddTokenToDictionary(word);

            wordsCount += words.Length;
        }

        private static string[] SplitFileContentByWords(string filePath)
        {
            return Regex.Split(File.ReadAllText(filePath), @"\W");
        }
        private static void AddTokenToDictionary(string word)
        {
            if (IsNotBlank(word))
                if (!tokens.Contains(word.ToLower()))
                    tokens.Add(word.ToLower());
                //if (!tokens.ContainsKey(word.ToLower()))
                //    tokens.Add(word.ToLower(), new List<int>());
        }

        private static bool IsNotBlank(string word)
        {
            return !(String.IsNullOrWhiteSpace(word) || String.IsNullOrEmpty(word));
        }

        private static void SaveDictionaryFile()
        {
            using (TextWriter textWriter = new StreamWriter(outputDictionaryFile))
            {
                foreach (string token in tokens)
                //foreach (string token in tokens.Keys)
                    textWriter.WriteLine(token);
            }
        }

    }
}
