using PropertiesStringifier;

namespace quick_sql.Model
{
    public class FilterBase : StringifyProperties
    {
        public string Server { get; set; } = "";
        public string Database { get; set; } = "";
    }
}
