using quick_sql.Model;
using System.Data;

namespace quick_sql.Service
{
    internal static class QueryService
    {
        public static async Task<Query> Run(QueryFilter filter, CancellationToken cancellationToken)
        {
            using DbService dbService = new(filter.Server, filter.Database);
            DataTable dataTable = await dbService.QueryAsync(filter.Query, cancellationToken);
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
