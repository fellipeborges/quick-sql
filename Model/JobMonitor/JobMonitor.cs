using PropertiesStringifier;
using System.Windows.Input;

namespace quick_sql.Model
{
    public class JobMonitor : StringifyProperties
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public bool Enabled { get; set; }
        public string EnabledDescriptive => Enabled ? "Yes" : "No";
        public ICommand? StartJobCommand { get; set; }
        public ICommand? StopJobCommand { get; set; }
    }
}
