using PropertiesStringifier;

namespace quick_sql.Model
{
    internal class Recent : StringifyProperties
    {
        public List<RecentItem> Items { get; set; } = [];
    }
}
