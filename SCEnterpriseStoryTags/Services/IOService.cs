using System;
using System.IO;

namespace SCEnterpriseStoryTags.Services
{
    public static class IOService
    {
        private static readonly string DataFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SharpCloudEnterpriseStoryTags");

        public static string ReadFromFile(string filename)
        {
            var fullPath = Path.Combine(DataFolderPath, filename);

            var data = !File.Exists(fullPath)
                ? string.Empty
                : File.ReadAllText(fullPath);

            return data;
        }

        public static void WriteToFile(string filename, string data, bool overwrite)
        {
            Directory.CreateDirectory(DataFolderPath);

            var file = Path.Combine(DataFolderPath, filename);

            if (overwrite)
            {
                File.WriteAllText(file, data);
            }
            else
            {
                File.AppendAllText(file, data);
            }
        }
    }
}
