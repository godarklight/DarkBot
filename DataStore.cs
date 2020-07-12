using System;
using System.IO;

namespace DarkBot
{
    public static class DataStore
    {
        private static string dataDir = Path.Combine(Environment.CurrentDirectory, "Data");

        public static string GetPath(string key)
        {
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            return Path.Combine(Environment.CurrentDirectory, "Data", key + ".txt");
        }

        public static string Load(string key)
        {
            string filePath = GetPath(key);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            return null;
        }

        public static void Save(string key, string value)
        {
            string filePath = GetPath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllText(filePath, value);
        }
    }
}