using quick_sql.Model;
using System.IO;
using System.Text.Json;

namespace quick_sql.Service
{
    internal static class CodeSnippetService
    {
        internal static List<CodeSnippet> Get()
        {
            string filePath = GetFilePath();
            List<CodeSnippet> codeSnippets = [];
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                codeSnippets = JsonSerializer.Deserialize<List<CodeSnippet>>(json) ?? [];
            }

            return codeSnippets;
        }

        internal static void Save(List<CodeSnippet> codeSnippets)
        {
            string filePath = GetFilePath();
            if (Directory.Exists(Path.GetDirectoryName(filePath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }

            File.WriteAllText(filePath, JsonSerializer.Serialize(codeSnippets, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static string GetFilePath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickSQL", "codesnippets.json");
    }
}
