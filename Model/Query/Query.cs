using PropertiesStringifier;
using System.Data;

namespace quick_sql.Model
{
    public class Query : StringifyProperties
    {
        public DataTable? Result { get; set; }
        public bool HasGridResult => Result != null && Result.Columns.Count > 0;
        public string Messages { get; set; } = "";
    }
}
