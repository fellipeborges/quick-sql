using PropertiesStringifier;

namespace quick_sql.Model
{
    public class CodeSnippet : StringifyProperties
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
    }
}
