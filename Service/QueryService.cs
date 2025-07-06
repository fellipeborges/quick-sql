using quick_sql.Model;
using System.Data;

namespace quick_sql.Service
{
    internal static class QueryService
    {
        public static Query Run(QueryFilter filter)
        {
            using DbService dbService = new(filter.Server, filter.Database);
            DataTable dataTable = dbService.Query(filter.Query);
            Query queryReturn = new();

            if (dataTable != null && dataTable.Columns.Count > 0)
            {
                queryReturn.Result = dataTable;
            }

            queryReturn.Messages = dbService.ConnectionMessages;
            return queryReturn;
        }
    }
}
