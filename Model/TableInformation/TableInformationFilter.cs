using PropertiesStringifier;

namespace quick_sql.Model
{
    public class TableInformationFilter : FilterBase
    {
        public string Table { get; set; } = "";
        public override string ToString() => this.StringifyProperties();
    }
}
