using PropertiesStringifier;

namespace quick_sql.Model
{
    public class TableInformation : StringifyProperties
    {
        public string Schema { get; set; } = "";
        public string Table { get; set; } = "";
        public decimal SpaceMb { get; set; } = 0;
        public long RowCount { get; set; } = 0;
        public DateOnly Creation { get; set; }
        public DateOnly LastWrite { get; set; }
        public DateOnly LastRead { get; set; }
    }
}
