using quick_sql.Model;

namespace quick_sql.Service
{
    internal static class TableInformationService
    {
        public static List<TableInformation> Search(TableInformationFilter filter)
        {
            using DbService dbService = new(filter.Server, filter.Database);
            string sql =
                @"
                    SELECT		[Schema]	= S.[Name],
			                    [Table]		= T.[Name],
			                    [RowCount]	= MAX(P.[Rows]),
			                    [SpaceMb]	= CAST(ROUND(((SUM(a.[total_pages]) * 8) / 1024.00), 2) AS NUMERIC(36, 2)),
                                [Creation]  = CAST(O.create_date AS DATE),
                                [LastWrite] = CAST(MAX(U.last_user_update) AS DATE),
                                [LastRead]  = CAST(ISNULL(MAX(U.last_user_seek), ISNULL(MAX(U.last_user_scan), MAX(U.last_user_lookup))) AS DATE)
                    FROM		sys.tables				    AS T
                    INNER JOIN	sys.indexes				    AS I ON T.[object_id]		= I.[object_id]
                    INNER JOIN	sys.partitions			    AS P ON I.[object_id]		= P.[object_id] AND I.[index_id] = P.[index_id]
                    INNER JOIN	sys.allocation_units	    AS A ON P.[partition_id]	= A.[container_id]
                    LEFT JOIN	sys.schemas				    AS S ON T.[schema_id]		= S.[schema_id]
                    LEFT JOIN	sys.objects				    AS O ON T.[object_id]       = O.[object_id]
                    LEFT JOIN	sys.dm_db_index_usage_stats AS U ON I.[object_id]       = U.[object_id] AND I.[index_id] = U.[index_id]
                    WHERE		T.is_ms_shipped = 0
                    $WHERE$
                    GROUP BY	T.[Name], S.[Name], O.create_date
                    ORDER BY	[SpaceMb] DESC, [Table] ASC
                ";

            string whereClause = "";
            if (!string.IsNullOrWhiteSpace(filter.Table))
            {
                whereClause = $"AND T.[Name] LIKE '%{filter.Table}%'";
            }

            sql = sql.Replace("$WHERE$", whereClause);
            List<TableInformation> ret = dbService.Query<TableInformation>(sql);

            return ret;
        }
    }
}
