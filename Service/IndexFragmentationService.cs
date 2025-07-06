using quick_sql.Model;

namespace quick_sql.Service
{
    internal static class IndexFragmentationService
    {
        public static List<IndexFragmentation> Search(IndexFragmentationFilter filter)
        {
            using DbService dbService = new(filter.Server, filter.Database);
            string sql =
                @"
                    SELECT      [Schema]            = SCHEMA_NAME(o.[schema_id]),
                                [Table]             = o.[name],
                                [Index]             = i.[name],
                                [FragPercentual]    = ips.avg_fragmentation_in_percent,
                                [PageCount]         = ips.page_count,
                                [RebuildScript]     = 'ALTER INDEX [' + i.[name] + '] ON [' + DB_NAME() + '].[' + SCHEMA_NAME(o.[schema_id]) + '].[' + o.[name] + '] ' + CASE WHEN ips.avg_fragmentation_in_percent > 30 THEN 'REBUILD;' ELSE 'REORGANIZE;' END
                    FROM        sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') AS ips
                    INNER JOIN  sys.indexes AS i ON ips.[object_id] = i.[object_id] AND ips.index_id = i.index_id
                    INNER JOIN  sys.objects AS o ON i.[object_id] = o.[object_id]
                    WHERE       o.[type] = 'U'
                    AND         i.[type] > 0
                    AND         o.is_ms_shipped = 0
                    AND         ips.avg_fragmentation_in_percent > 0
                    $WHERE$
                    ORDER BY    CAST(ips.avg_fragmentation_in_percent AS INT) DESC, ips.page_count DESC
                ";

            string whereClause = "";
            if (!string.IsNullOrWhiteSpace(filter.Table))
            {
                whereClause = $"AND o.[Name] LIKE '%{filter.Table}%'";
            }

            sql = sql.Replace("$WHERE$", whereClause);
            List<IndexFragmentation> ret = dbService.Query<IndexFragmentation>(sql);

            return ret;
        }

        public static void ExecuteRebuildCommand(string server, string database, string rebuildScript)
        {
            using DbService dbService = new(server, database);
            dbService.ExecuteNonQuery(rebuildScript);
        }
    }
}
