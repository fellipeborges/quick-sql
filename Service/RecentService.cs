using quick_sql.Model;
using System.IO;
using System.Text.Json;

namespace quick_sql.Service
{
    internal static class RecentService
    {
        internal static Recent Get()
        {
            Recent recent = new();
            string filePath = GetFilePath();
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                recent = JsonSerializer.Deserialize<Recent>(json) ?? new Recent();
            }

            return recent;
        }

        internal static void Save(Recent recent)
        {
            string filePath = GetFilePath();
            if (Directory.Exists(Path.GetDirectoryName(filePath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }

            File.WriteAllText(filePath, JsonSerializer.Serialize(recent, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static string GetFilePath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickSQL", "recents.json");
    }
}
