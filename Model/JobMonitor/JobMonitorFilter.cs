using PropertiesStringifier;

namespace quick_sql.Model
{
    public class JobMonitorFilter : FilterBase
    {
        public string? Name { get; set; }
        public bool EnabledYes { get; set; }
        public bool EnabledNo { get; set; }
        public override string ToString() => this.StringifyProperties();
    }
}
