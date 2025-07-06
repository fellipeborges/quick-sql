using PropertiesStringifier;
using System.Windows.Input;

namespace quick_sql.Model
{
    public class ExpensiveQuery : StringifyProperties
    {
        public int SPID { get; set; }
        public string Database { get; set; } = "";
        public string Host { get; set; } = "";
        public string Login { get; set; } = "";
        public string Program { get; set; } = "";
        public string ElapsedTime { get; set; } = "";
        public long Cost { get; set; }
        public int Blocking { get; set; }
        public string BlockedBy { get; set; } = "";
        public string Query { get; set; } = "";
        public ICommand? KillSessionCommand { get; set; }
        public ICommand? GotoBlockerSessionCommand { get; set; }
        public ICommand? QueryViewCommand { get; set; }
        public ICommand? QueryCopyCommand { get; set; }
    }
}
