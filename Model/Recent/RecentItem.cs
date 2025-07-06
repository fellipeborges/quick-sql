using PropertiesStringifier;
using quick_sql.Enum;

namespace quick_sql.Model
{
    internal class RecentItem : StringifyProperties
    {
        public RecentTypeEnum Type { get; set; }
        public string Value { get; set; } = "";
        public bool LastUsed { get; set; }
    }
}
