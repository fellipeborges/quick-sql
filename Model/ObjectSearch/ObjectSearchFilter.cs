using PropertiesStringifier;

namespace quick_sql.Model
{
    public class ObjectSearchFilter : FilterBase
    {
        public string Term { get; set; } = "";
        public bool SearchInName { get; set; } = false;
        public bool SearchInCode { get; set; } = false;
        public override string ToString() => this.StringifyProperties();
    }
}
