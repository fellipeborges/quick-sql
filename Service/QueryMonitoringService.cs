using quick_sql.Model;

namespace quick_sql.Service
{
    internal static class QueryMonitoringService
    {
        public static async Task<List<QueryMonitoring>> SearchAsync(QueryMonitoringFilter filter, CancellationToken cancellationToken)
        {
            using DbService dbService = new(filter.Server);
            string sql =
                @"  
                    IF OBJECT_ID('tempdb..#TMP_BLOCKS') IS NOT NULL DROP TABLE #TMP_BLOCKS
                    CREATE TABLE #TMP_BLOCKS ([session_id] INT, [Status] VARCHAR(MAX), [LOGIN] VARCHAR(MAX), [HostName] VARCHAR(MAX), [BlkBy] VARCHAR(MAX), [DBName] VARCHAR(MAX), [Command] VARCHAR(MAX), [CPUTime] BIGINT, [DiskIO] BIGINT, [LastBatch] VARCHAR(MAX), [ProgramName] VARCHAR(MAX), [SPID_1] BIGINT, [REQUESTID] BIGINT)
                    INSERT INTO #TMP_BLOCKS EXEC sp_who2
                    UPDATE #TMP_BLOCKS SET BlkBy = NULL WHERE BlkBy = '  .'

                    IF OBJECT_ID('tempdb..#TMP_SESSIONS') IS NOT NULL DROP TABLE #TMP_SESSIONS
                    SELECT		[SPID]			    = exs.[session_id],
			                    [Status]			= LTRIM(RTRIM(UPPER(LEFT(exs.[status], 1)) + LOWER(SUBSTRING(exs.[status], 2, LEN(exs.[status]))))),
                                [Database]			= DB_NAME(exs.[database_id]),
			                    [Host]				= exs.[host_name],
			                    [Login]			    = exs.[login_name],
			                    [Program]			= exs.[program_name],
			                    [Cost]				= ISNULL(exs.[reads], 0) + ISNULL(exs.[logical_reads], 0) + ISNULL(exs.[writes], 0) + ISNULL(exs.[cpu_time], 0) +
								                      ISNULL(exr.[reads], 0) + ISNULL(exr.[logical_reads], 0) + ISNULL(exr.[writes], 0) + ISNULL(exr.[cpu_time], 0),
			                    [ElapsedTime]	    = CASE
									                    WHEN ISNULL(exs.[total_elapsed_time], 0) > 0 THEN CONVERT(VARCHAR, DATEADD(SECOND, exs.[total_elapsed_time] / 1000, '00:00:00'), 108)
									                    WHEN exr.[start_time] IS NOT NULL THEN CONVERT(VARCHAR, DATEADD(SECOND, DATEDIFF(SECOND, exr.[start_time], GETDATE()), '00:00:00'), 108)
									                    ELSE ''
								                      END,
			                    [Blocking]	        = CAST(0 AS INT),
			                    [BlockedBy]		    = CAST('' AS VARCHAR(10)),
			                    [Query]			    = CAST('' AS VARCHAR(MAX))
                    INTO		#TMP_SESSIONS
                    FROM		[sys].[dm_exec_sessions]    AS exs
                    LEFT JOIN	[sys].[dm_exec_requests]    AS exr ON exr.[session_id] = exs.[session_id]
                    WHERE		exs.[is_user_process] = 1
                    AND			exs.[session_id] <> @@SPID
                    $WHERE_DM_EXEC$

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
                    $WHERE_GENERAL$
                    ORDER BY	$ORDER_BY$
                ";

            string whereDmExecClause = string.Empty;
            if (!string.IsNullOrWhiteSpace(filter.Database))
                whereDmExecClause += $" AND DB_NAME(exs.[database_id]) LIKE '{filter.Database}'";

            if (!string.IsNullOrWhiteSpace(filter.Host))
                whereDmExecClause += $" AND exs.[host_name] LIKE '{filter.Host}'";

            if (!string.IsNullOrWhiteSpace(filter.Login))
                whereDmExecClause += $" AND exs.[login_name] LIKE '{filter.Login}'";

            if (!string.IsNullOrWhiteSpace(filter.Program))
                whereDmExecClause += $" AND exs.[program_name] LIKE '{filter.Program}'";

            if (filter.RunningOnly == true)
                whereDmExecClause += $" AND LTRIM(RTRIM(exs.[status])) = ('running')";

            string whereGeneralClause = string.Empty;
            if (filter.BlockingOnly == true)
                whereGeneralClause += $" AND ([Blocking] > 0 OR [BlockedBy] <> '')";

            if (!string.IsNullOrWhiteSpace(filter.Query))
                whereGeneralClause += $" AND [Query] LIKE '{filter.Query}'";

            string orderBy = "[cost] DESC";
            if (filter.BlockingOnly == true)
                orderBy = "[Blocking] DESC, [cost] DESC";

            sql = sql.Replace("$WHERE_DM_EXEC$", whereDmExecClause);
            sql = sql.Replace("$WHERE_GENERAL$", whereGeneralClause);
            sql = sql.Replace("$ORDER_BY$", orderBy);

            List<QueryMonitoring> ret = await dbService.QueryAsync<QueryMonitoring>(sql, cancellationToken);
            return ret;
        }

        public static void KillSession(string server, int spid)
        {
            using DbService dbService = new(server);
            dbService.ExecuteNonQuery($"KILL {spid};");
        }
    }
}
