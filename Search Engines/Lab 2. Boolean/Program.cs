using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.Default;
            Console.OutputEncoding = System.Text.Encoding.Default;
            DateTime startTime = DateTime.UtcNow;
            RemoveSystemFiles();

            SetCollectionFolder(args);
            TaxonomizeCollection();

            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Indexing started.");
            IndexCollection();

            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Indexing completed in " + Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2) + " seconds.");
            
            SaveSystemFiles();
            
            Console.WriteLine();
            string searchTip = "Type your search request (single words; AND, OR, NOT operators; no grouping; case insensitive; EXIT to leave): ";
            Console.WriteLine(searchTip);
            string request = Console.ReadLine();
            
            while (!request.Trim().ToLower().Equals("exit"))
            {
                Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Bingo! Search request: '" + request + "'");
                Console.WriteLine();
                
                
                DateTime searchStart = DateTime.UtcNow;
                Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Results (incident matrix):");
                IncidenceMatrixSearch(request);
                Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Search (incident matrix) completed in " + Math.Round((DateTime.UtcNow - searchStart).TotalMilliseconds, 2) + " milliseconds.");
                Console.WriteLine();

                searchStart = DateTime.UtcNow;
                Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Results (inverted index):");
                InvertedIndexSearch(request);
                Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Search (inverted index) completed in " + Math.Round((DateTime.UtcNow - searchStart).TotalMilliseconds, 2) + " milliseconds.");
                Console.WriteLine();

                Console.WriteLine();
                Console.WriteLine(searchTip);
                request = Console.ReadLine();
            }
        }


        // Preparing the files 
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
            for (int fileNumber = 0; fileNumber < collectionSize; fileNumber++)
            {
                string filePath = fileCollection[fileNumber];
                Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " Indexing file: " + filePath);

                string[] words = SplitFileContentByWords(filePath);
                foreach (string word in words)
                    AddTokenToIndexes(Tokenize(word), fileNumber);
                wordsCount += words.Length;
            }
        }
        private static string[] SplitFileContentByWords(string filePath)
        {
            return Regex.Split(File.ReadAllText(filePath, System.Text.Encoding.Default), @"\W");
        }
        private static string Tokenize(string word)
        {
            //return Regex.Replace(word.ToLower(), "[_0-9]", string.Empty);
            return Regex.Replace(word.ToLower(), "[_]", string.Empty);
        }


        // Filling in structures
        private static void AddTokenToIndexes(string token, int fileNumber)
        {
            if (String.IsNullOrWhiteSpace(token) || String.IsNullOrEmpty(token))
                return;

            AddTokenToDictionary(token);
            AddTokenToIncidenceMatrix(token, fileNumber);
            AddTokenToInvertedIndex(token, fileNumber);
        }
        
        private static void AddTokenToDictionary(string token)
        {
            if (!dictionary.Contains(token))
                dictionary.Add(token);
        }

        private static void AddTokenToIncidenceMatrix(string token, int fileNumber)
        {
            if (!incidenceMatrix.ContainsKey(token))
                incidenceMatrix.Add(token, new bool[collectionSize]);

            incidenceMatrix[token][fileNumber] = true;
        }

        private static void AddTokenToInvertedIndex(string token, int fileNumber)
        {
            if (!invertedIndex.ContainsKey(token))
                invertedIndex.Add(token, new List<int>());

            if (!invertedIndex[token].Contains(fileNumber))
            {
                invertedIndex[token].Add(fileNumber);
                invertedIndex[token].Sort();
            }
        }

        // Search engines
        private static void IncidenceMatrixSearch(string request)
        {
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
                            for (int i = 0; i < collectionSize; i++)
                                currentMatch[i] = incidenceMatrix[token][i];
                        }
                        catch
                        { }

                        if (not)
                            currentMatch = currentMatch.Select(x => !x).ToArray();

                        if (!and && !or)
                            searchResult = currentMatch;
                        else if (and)
                            searchResult = searchResult.SelectMany(x => currentMatch, (x, y) => x && y).ToArray();
                        else if (or)
                            searchResult = searchResult.SelectMany(x => currentMatch, (x, y) => x || y).ToArray();

                        or = false;
                        and = false;
                        not = false;
                        break;
                }
            }

            IncidenceMatrixPrintResults(searchResult);
        }

        private static void InvertedIndexSearch(string request)
        {
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

            InvertedIndexPrintResults(searchResult);
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


        // Printing results
        private static void IncidenceMatrixPrintResults(bool[] incidenceArray)
        {
            for (int fileNumber = 0; fileNumber < collectionSize; fileNumber++)
               if (incidenceArray[fileNumber]) 
                Console.WriteLine(fileCollection[fileNumber].ToString());
        }

        private static void InvertedIndexPrintResults(List<int> fileNumbers)
        {
            foreach (int fileNumber in fileNumbers)
                Console.WriteLine(fileCollection[fileNumber].ToString());
        }




        // System methods
        private static void SetCollectionFolder(string[] args)
        {
            if (args.Length > 0)
                collectionFolder = args[0].TrimEnd('\\');
        }

        static void RemoveSystemFiles()
        {
            if (Directory.Exists(systemFolder))
                Directory.Delete(systemFolder, true);
        }

        private static void SaveSystemFiles()
        {
            Directory.CreateDirectory(systemFolder);
            Console.WriteLine();
            Console.WriteLine("Collection size (words count): " + wordsCount + ".");
            Console.WriteLine("Dictionary size (tokens count): " + dictionary.Count + ".");
            Console.WriteLine("System files sizes:");

            string fileName = systemFolder + "\\dictionary.json";
            File.WriteAllText(fileName, JsonConvert.SerializeObject(dictionary, Formatting.Indented));
            Console.WriteLine(fileName + "  " + System.Math.Round((decimal)(new FileInfo(fileName)).Length / 1024) + " KB");

            fileName = systemFolder + "\\incidence_matrix.json";
            SortedDictionary<string, List<byte>> incidenceLists = new SortedDictionary<string, List<byte>>();
            foreach(KeyValuePair<string, bool[]> incidenceArray in incidenceMatrix)
                incidenceLists.Add(incidenceArray.Key, incidenceArray.Value.Select(x => Convert.ToByte(x)).ToList());
            File.WriteAllText(fileName, JsonConvert.SerializeObject(incidenceLists, Formatting.Indented));
            Console.WriteLine(fileName + "  " + System.Math.Round((decimal)(new FileInfo(fileName)).Length / 1024) + " KB");

            fileName = systemFolder + "\\inverted_index.json";
            File.WriteAllText(fileName, JsonConvert.SerializeObject(invertedIndex, Formatting.Indented));
            Console.WriteLine(fileName + "  " + System.Math.Round((decimal)(new FileInfo(fileName)).Length / 1024) + " KB");

        }
    }

    public class SearchResult
    {

    }
}
