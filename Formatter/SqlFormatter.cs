using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace quick_sql.Converter
{
    public static class SqlFormatter
    {
        public static void Format(string sqlCode, RichTextBox richTextBox, string highlightText = "")
        {
            var keywords = new HashSet<string>
                {
                    "ADD", "ALL", "ALTER", "AND", "ANY", "AS", "ASC",
                    "BACKUP", "BEGIN", "BETWEEN", "BREAK", "BULK", "BY",
                    "CASCADE", "CASE", "CHARINDEX", "CLOSE", "COALESCE", "COLLATE", "COLUMN", "COMMIT",
                    "CONTAINS", "CONTINUE", "CONVERT", "CREATE", "CROSS", "CURSOR",
                    "DATABASE", "DBCC", "DEALLOCATE", "DECLARE", "DEFAULT", "DELETE", "DENY", "DESC", "DISTINCT", "DOUBLE", "DROP",
                    "ELSE", "END", "ERROR_MESSAGE", "ESCAPE", "EXEC", "EXECUTE", "EXISTS", "EXIT",
                    "FETCH", "FOR", "FROM", "FULL", "FUNCTION",
                    "GO", "GOTO", "GROUP", "HAVING",
                    "IDENTITY", "IF", "IN", "INDEX", "INNER", "INSERT", "INTO", "IS",
                    "JOIN",
                    "KILL",
                    "LEFT", "LIKE", "LTRIM",
                    "MERGE",
                    "NOT", "NULL", "NULLIF",
                    "OBJECT_ID", "OFF", "OFFSETS", "ON", "OPEN", "OPENQUERY", "OPENROWSET", "OPENXML", "OPTION", "OR", "ORDER", "OUTER", "OVER", "OUTPUT",
                    "PRINT", "PROC", "PROCEDURE",
                    "RAISERROR", "REBUILD", "RETURN", "RIGHT", "ROLLBACK", "ROWCOUNT", "RTRIM",
                    "SELECT", "SET", "SP_HELPFILE", "SP_HELP", "STRING_AGG",
                    "TABLE", "THEN", "THROW", "TOP", "TRAN", "TRANSACTION", "TRIGGER", "TRUNCATE", "TRY_CONVERT", "TRY_CAST",
                    "UNION", "UPDATE", "USE",
                    "VALUES", "VIEW",
                    "WAITFOR", "WHEN", "WHERE", "WHILE", "WITH",
                    "XP_CMDSHELL"
                };

            richTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph();
            var lines = sqlCode.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            bool hasHighlightText = !string.IsNullOrWhiteSpace(highlightText);
            string highlightTextLower = hasHighlightText ? highlightText.ToLowerInvariant() : string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                string[] tokens = Regex.Split(lines[i], @"(\W)");
                foreach (var token in tokens)
                {
                    if (string.IsNullOrEmpty(token))
                        continue;

                    if (hasHighlightText && token.Contains(highlightTextLower, StringComparison.InvariantCultureIgnoreCase))
                    {
                        int startIndex = token.IndexOf(highlightTextLower, StringComparison.InvariantCultureIgnoreCase);
                        if (startIndex > 0)
                        {
                            var runBefore = new Run(token.Substring(0, startIndex));
                            ApplyFormatting(runBefore, token, keywords);
                            paragraph.Inlines.Add(runBefore);
                        }

                        var highlightedPart = new Run(token.Substring(startIndex, highlightText.Length));
                        ApplyFormatting(highlightedPart, token, keywords);
                        highlightedPart.Background = Brushes.Yellow;
                        paragraph.Inlines.Add(highlightedPart);

                        if (startIndex + highlightText.Length < token.Length)
                        {
                            var runAfter = new Run(token.Substring(startIndex + highlightText.Length));
                            ApplyFormatting(runAfter, token, keywords);
                            paragraph.Inlines.Add(runAfter);
                        }
                    }
                    else
                    {
                        var run = new Run(token);
                        ApplyFormatting(run, token, keywords);
                        paragraph.Inlines.Add(run);
                    }
                }

                if (i < lines.Length - 1)
                    paragraph.Inlines.Add(new LineBreak());
            }

            richTextBox.Document.Blocks.Add(paragraph);
        }

        private static void ApplyFormatting(Run run, string token, HashSet<string> keywords)
        {
            if (keywords.Contains(token.ToUpperInvariant()))
            {
                run.Foreground = Brushes.Blue;
                run.FontWeight = FontWeights.Bold;
            }
        }
    }
}
