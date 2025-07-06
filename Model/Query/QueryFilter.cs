using PropertiesStringifier;

namespace quick_sql.Model
{
    public class QueryFilter : FilterBase
    {
        public string Query { get; set; } = "";
        public override string ToString() => this.StringifyProperties();
    }
}
