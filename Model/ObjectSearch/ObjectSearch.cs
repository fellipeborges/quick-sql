using PropertiesStringifier;
using System.Windows.Input;

namespace quick_sql.Model
{
    public class ObjectSearch : StringifyProperties
    {
        public string Database { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public ICommand? CodeViewCommand { get; set; }
        public ICommand? CodeCopyCommand { get; set; }
    }
}
