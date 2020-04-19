using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace Ranking
{
    class Program
    {
        public static string collectionFolder = "F:\\NaUKMA\\Repos\\Search Engines\\Collections\\xml";
        public static readonly string systemFolder = ".system";

        public static readonly string authorZone = "author";
        public static readonly string nameZone = "book-title";
        public static readonly string contentZone = "body";

        public static Dictionary<string, int> zoneWeights = new Dictionary<string, int> { [authorZone] = 5, [nameZone] = 3, [contentZone] = 2 };
        public static Dictionary<int, string> fileCollection = new Dictionary<int, string>();
        public static SortedDictionary<string, List<string>> invertedIndex = new SortedDictionary<string, List<string>>();

        public static readonly string searchInstruction =
            "\n\nType your search request (multiple words; no operators; case insensitive; EXIT to leave):\n>>> ";

        static void Main(string[] args)
        {
            SetCollectionParameters(args);
            ClearSystemFiles();

            TaxonomizeCollection();
            IndexCollection();

            Console.Write(searchInstruction);
            Console.OutputEncoding = Encoding.UTF8;
            string request = Console.ReadLine();

            while (!request.Trim().ToLower().Equals("exit"))
            {
                InvertedIndexSearch(request);
                Console.Write(searchInstruction);
                request = Console.ReadLine();
            }
        }

        // Indexing
        private static void SetCollectionParameters(string[] args)
        {
            if (args.Length > 0)
                collectionFolder = args[0].TrimEnd('\\');
            else
            {
                Console.WriteLine(String.Format("Enter the folder that stores collection or press ENTER to use default ('{0}'): ", collectionFolder));
                string consoleInput = Console.ReadLine();
                if (!String.IsNullOrEmpty(consoleInput))
                    collectionFolder = consoleInput.TrimEnd('\\');
            }

            List<string> zones = new List<string>(zoneWeights.Keys);
            foreach (var zone in zones)
            {
                AskUpdateRankingWeight(zone);
                Console.WriteLine(zoneWeights[zone]);
            }

        }
        private static void AskUpdateRankingWeight(string zone)
        {
            Console.WriteLine(String.Format("Enter '{0}' zone ranking weight or press ENTER to use default ({1}): ", zone, zoneWeights[zone]));
            string consoleInput = Console.ReadLine();
            if (!String.IsNullOrEmpty(consoleInput))
                zoneWeights[zone] = Int32.Parse(consoleInput);
        }

        static void ClearSystemFiles()
        {
            if (Directory.Exists(collectionFolder + '\\' + systemFolder))
                Directory.Delete(collectionFolder + '\\' + systemFolder, true);

            Directory.CreateDirectory(collectionFolder + '\\' + systemFolder);
        }

        private static void TaxonomizeCollection()
        {
            int fileNumber = 0;
            foreach (string filePath in Directory.GetFiles(collectionFolder))
                fileCollection.Add(fileNumber++, filePath);
        }

        private static void IndexCollection()
        {
            DateTime startTime = DateTime.UtcNow;
            foreach (var fileNumber in fileCollection.Keys)
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(fileCollection[fileNumber]);

                UpdateInvertedIndex(ParseXML(xmlDocument.SelectSingleNode("//title-info/author")), fileNumber, authorZone);
                UpdateInvertedIndex(ParseXML(xmlDocument.SelectSingleNode("//title-info/book-title")), fileNumber, nameZone);
                UpdateInvertedIndex(ParseXML(xmlDocument.SelectSingleNode("//body")), fileNumber, contentZone);
            }

            Console.WriteLine("\nInverted index created in " + Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2) + " seconds:");

            string jsonFile = collectionFolder + '\\' + systemFolder + "\\inverted_index.json";
            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(invertedIndex, Newtonsoft.Json.Formatting.Indented));
            Console.WriteLine("> " + jsonFile + "  " + Math.Round((decimal)(new FileInfo(jsonFile)).Length / 1024) + " KB");
        }

        private static List<string> ParseXML(XmlNode xmlNode)
        {
            return ParseNode(xmlNode, new List<string>());
        }

        private static List<string> ParseNode(XmlNode xmlNode, List<string> words)
        {
            string innerText = xmlNode.InnerText.ToString();
            if (!String.IsNullOrEmpty(innerText)) 
                words.AddRange(ParseText(innerText));

            if (xmlNode.HasChildNodes)
                foreach (XmlNode childNode in xmlNode.ChildNodes)
                    words.AddRange(ParseNode(childNode, words));

            return words;
        }
        private static List<string> ParseText(string text)
        {
            return Regex.Split(text, @"\W").ToList();
        }

        private static void UpdateInvertedIndex(List<string> words, int fileNumber, string zone)
        {
            foreach (string word in words)
                AddTokenToInvertedIndex(Tokenize(word), fileNumber, zone);
        }

        private static string Tokenize(string word)
        {
            //return Regex.Replace(word.ToLower(), "[_0-9]", string.Empty);
            return Regex.Replace(word.ToLower(), "[_]", string.Empty);
        }

        private static void AddTokenToInvertedIndex(string token, int fileNumber, string zone)
        {
            if (String.IsNullOrWhiteSpace(token) || String.IsNullOrEmpty(token))
                return;

            if (!invertedIndex.ContainsKey(token))
                invertedIndex.Add(token, new List<string>());

            string fileZone = String.Format(@"{0}.{1}", fileNumber, zone);
            string zoneRegex = String.Format(@"(^|,){0}.*?(,|$)", fileZone);
            if (!invertedIndex[token].Exists(x => Regex.IsMatch(x, zoneRegex)))
            {
                string fileRegex = String.Format(@"(^|,){0}.*?(\.)", fileNumber);
                if (invertedIndex[token].Exists(x => Regex.IsMatch(x, fileRegex)))
                {
                    int index = invertedIndex[token].FindIndex(x => Regex.IsMatch(x, fileRegex));
                    invertedIndex[token][index] += String.Format(@",{0}", fileZone);
                }
                else
                    invertedIndex[token].Add(fileZone);

                invertedIndex[token].Sort();
            }
        }

        // Search engine
        private static void InvertedIndexSearch(string request)
        {
            DateTime searchStart = DateTime.UtcNow;

            Dictionary<int, int> fileRanking = new Dictionary<int, int>();
            foreach (int fileNumber in fileCollection.Keys)
                fileRanking.Add(fileNumber, 0);

            string[] requestParts = request.Split(' ');
            foreach (string requestPart in requestParts)
            {
                string token = Tokenize(requestPart);
                if (invertedIndex.ContainsKey(token))
                {
                    List<string> matchMaps = new List<string>(invertedIndex[token]);
                    foreach (var matchMap in matchMaps)
                    {
                        foreach (var fileZone in matchMap.Split(','))
                        {
                            int fileNumber = Int32.Parse(fileZone.Split('.')[0]);
                            string zone = fileZone.Split('.')[1];
                            fileRanking[fileNumber] += zoneWeights[zone];
                        }
                    }
                }
            }

            InvertedIndexPrintResults(fileRanking, Math.Round((DateTime.UtcNow - searchStart).TotalMilliseconds, 2));
        }

        private static void InvertedIndexPrintResults(Dictionary<int, int> fileRankings, double durationMs)
        {
            Console.WriteLine("\nSearch results (inverted index), completed in " + durationMs + " ms:");
            foreach (var fileRanking in fileRankings.OrderByDescending(x => x.Value))
                if (fileRanking.Value != 0)
                    Console.WriteLine("> " + fileCollection[fileRanking.Key].ToString());

        }
    }
}
