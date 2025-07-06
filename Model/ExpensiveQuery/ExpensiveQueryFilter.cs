using PropertiesStringifier;

namespace quick_sql.Model
{
    public class ExpensiveQueryFilter : FilterBase
    {
        public string? Host { get; set; }
        public string? Login { get; set; }
        public string? Program { get; set; }
        public bool? BlockingOnly { get; set; }
        public string? Query { get; set; }
        public override string ToString() => this.StringifyProperties();
    }
}
