using quick_sql.Model;

namespace quick_sql.Service
{
    internal static class ExpensiveQueryService
    {
        public static async Task<List<ExpensiveQuery>> SearchAsync(ExpensiveQueryFilter filter, CancellationToken cancellationToken)
        {
            using DbService dbService = new(filter.Server);
            string sql =
                @"  
                    IF OBJECT_ID('tempdb..#TMP_BLOCKS') IS NOT NULL DROP TABLE #TMP_BLOCKS
                    CREATE TABLE #TMP_BLOCKS ([session_id] INT, [Status] VARCHAR(MAX), [LOGIN] VARCHAR(MAX), [HostName] VARCHAR(MAX), [BlkBy] VARCHAR(MAX), [DBName] VARCHAR(MAX), [Command] VARCHAR(MAX), [CPUTime] BIGINT, [DiskIO] BIGINT, [LastBatch] VARCHAR(MAX), [ProgramName] VARCHAR(MAX), [SPID_1] BIGINT, [REQUESTID] BIGINT)
                    INSERT INTO #TMP_BLOCKS EXEC sp_who2
                    UPDATE #TMP_BLOCKS SET BlkBy = NULL WHERE BlkBy = '  .'

                    IF OBJECT_ID('tempdb..#TMP_SESSIONS') IS NOT NULL DROP TABLE #TMP_SESSIONS
                    SELECT		[SPID]			    = [session_id],
			                    [Database]			= DB_NAME(database_id),
			                    [Host]				= [host_name],
			                    [Login]			    = [login_name],
			                    [Program]			= [program_name],
			                    [Cost]				= [reads] + [logical_reads] + [writes] + [cpu_time],
			                    [ElapsedTime]	    = CONVERT(VARCHAR, DATEADD(ms, [total_elapsed_time], '00:00:00'), 108),
			                    [Blocking]	        = CAST(0 AS INT),
			                    [BlockedBy]		    = CAST('' AS VARCHAR(10)),
			                    [Query]			    = CAST('' AS VARCHAR(MAX))
                    INTO		#TMP_SESSIONS
                    FROM		[sys].[dm_exec_sessions]
                    WHERE		[is_user_process] = 1
                    AND			[session_id] <> @@SPID

                    DECLARE @INPUTBUFFER_TABLE TABLE ([EventType] VARCHAR(100), [Parameters] VARCHAR(100), [EventInfo] VARCHAR(MAX))
                    DECLARE @sql_cmd VARCHAR(1000)
                    DECLARE @CURSOR_session_id INT
                    DECLARE CURSOR_SESSIONS CURSOR LOCAL FOR (SELECT [SPID] FROM #TMP_SESSIONS)
                    OPEN CURSOR_SESSIONS
                    FETCH NEXT FROM CURSOR_SESSIONS INTO @CURSOR_session_id
                    WHILE (@@FETCH_STATUS = 0)
                    BEGIN
	                    BEGIN TRY
		                    SET @sql_cmd = 'DBCC INPUTBUFFER(' + CONVERT(VARCHAR, @CURSOR_session_id) + ') WITH NO_INFOMSGS'
		                    DELETE FROM @INPUTBUFFER_TABLE
		                    INSERT INTO @INPUTBUFFER_TABLE
		                    EXEC (@sql_cmd);

		                    UPDATE	#TMP_SESSIONS
		                    SET		[Query]		= ISNULL((SELECT TOP 1 [EventInfo] FROM @INPUTBUFFER_TABLE), ''),
				                    [Blocking]	= (SELECT COUNT(DISTINCT X.[session_id]) FROM #TMP_BLOCKS AS X WHERE BlkBy = [SPID]),
				                    [BlockedBy]	= ISNULL((SELECT TOP 1 LTRIM(RTRIM(BlkBy)) FROM #TMP_BLOCKS WHERE [session_id] = [SPID] AND BlkBy IS NOT NULL), '')
		                    WHERE	[SPID] = @CURSOR_session_id
	                    END TRY
	                    BEGIN CATCH
	                    END CATCH

	                    FETCH NEXT FROM CURSOR_SESSIONS INTO @CURSOR_session_id
                    END
                    CLOSE CURSOR_SESSIONS
                    DEALLOCATE CURSOR_SESSIONS

                    SELECT		*
                    FROM		#TMP_SESSIONS
                    WHERE		1 = 1
                    $WHERE$
                    ORDER BY	$ORDER_BY$
                ";

            string whereClause = string.Empty;
            if (!string.IsNullOrWhiteSpace(filter.Database))
                whereClause += $" AND [Database] LIKE '{filter.Database}'";

            if (!string.IsNullOrWhiteSpace(filter.Host))
                whereClause += $" AND [Host] LIKE '{filter.Host}'";

            if (!string.IsNullOrWhiteSpace(filter.Login))
                whereClause += $" AND [Login] LIKE '{filter.Login}'";

            if (!string.IsNullOrWhiteSpace(filter.Program))
                whereClause += $" AND [Program] LIKE '{filter.Program}'";

            if (filter.BlockingOnly == true)
                whereClause += $" AND ([Blocking] > 0 OR [BlockedBy] <> '')";

            if (!string.IsNullOrWhiteSpace(filter.Query))
                whereClause += $" AND [Query] LIKE '{filter.Query}'";

            string orderBy = "[cost] DESC";
            if (filter.BlockingOnly == true)
                orderBy = "[Blocking] DESC, [cost] DESC";

            sql = sql.Replace("$WHERE$", whereClause);
            sql = sql.Replace("$ORDER_BY$", orderBy);

            List<ExpensiveQuery> ret = await dbService.QueryAsync<ExpensiveQuery>(sql, cancellationToken);
            return ret;
        }

        public static void KillSession(string server, int spid)
        {
            using DbService dbService = new(server);
            dbService.ExecuteNonQuery($"KILL {spid};");
        }
    }
}
