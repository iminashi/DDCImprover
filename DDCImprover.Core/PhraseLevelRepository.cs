using Rocksmith2014Xml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DDCImprover.Core
{
    public static class PhraseLevelRepository
    {
        private static readonly string repositoryFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phraselevels.zip");

        private static readonly ConcurrentDictionary<string, string> queue = new ConcurrentDictionary<string, string>();

        private static string GetHash(string filename)
        {
            var hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(filename));
            return hash.Aggregate(new StringBuilder(), (sb, b) => sb.Append(b.ToString("x2"))).ToString();
        }

        public static Dictionary<string, int> TryGetLevels(string filename)
        {
            if (!File.Exists(repositoryFile))
                return null;

            string entryName = GetHash(filename);

            using (ZipArchive archive = ZipFile.OpenRead(repositoryFile))
            {
                var entry = archive.GetEntry(entryName);
                if (entry != null)
                {
                    var dict = new Dictionary<string, int>();

                    using (StreamReader reader = new StreamReader(entry.Open()))
                    {
                        string content = reader.ReadToEnd();
                        var pairs = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < pairs.Length; i += 2)
                        {
                            dict.Add(pairs[i], int.Parse(pairs[i + 1]));
                        }

                        return dict;
                    }
                }
            }

            return null;
        }

        private static string CreatePhraseLevelsString(PhraseCollection phrases)
        {
            var sb = new StringBuilder();

            foreach (var phrase in phrases)
            {
                // format: "p1 10 p2 4"
                sb.Append(phrase.Name).Append(" ").Append(phrase.MaxDifficulty).Append(" ");
            }

            return sb.ToString();
        }

        public static void QueueForSave(string filename, PhraseCollection phrases)
        {
            string entryName = GetHash(filename);
            string levels = CreatePhraseLevelsString(phrases);

            queue.TryAdd(entryName, levels);
        }

        public static void UpdateRepository()
        {
            if (queue.Count == 0)
                return;

            using (ZipArchive archive = ZipFile.Open(repositoryFile, ZipArchiveMode.Update))
            {
                foreach (var keyVal in queue)
                {
                    // Delete old entry if one exists
                    archive.GetEntry(keyVal.Key)?.Delete();

                    var entry = archive.CreateEntry(keyVal.Key);
                    using (StreamWriter writer = new StreamWriter(entry.Open()))
                    {
                        writer.Write(keyVal.Value);
                    }
                }
            }

            queue.Clear();
        }
    }
}
