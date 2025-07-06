using PropertiesStringifier;
using System.Windows.Input;

namespace quick_sql.Model
{
    public class IndexFragmentation : StringifyProperties
    {
        public string Schema { get; set; } = "";
        public string Table { get; set; } = "";
        public string Index { get; set; } = "";
        public double FragPercentual { get; set; } = 0;
        public int PageCount { get; set; } = 0;
        public string RebuildScript { get; set; } = "";
        public ICommand? RebuildScriptViewCommand { get; set; }
        public ICommand? RebuildScriptCopyCommand { get; set; }
        public ICommand? RebuildScriptExecCommand { get; set; }
    }
}
