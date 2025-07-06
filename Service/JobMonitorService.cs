using quick_sql.Model;

namespace quick_sql.Service
{
    internal static class JobMonitorService
    {
        public static List<JobMonitor> Search(JobMonitorFilter filter)
        {
            using DbService dbService = new(filter.Server, "msdb");
            string sql =
                @"  
                    ;WITH cte_jobs AS (
                        SELECT  [Id]                = jobs.[job_id],
                                [Name]              = jobs.[name],
                                [Status]            = CASE WHEN last_activity.StartExecutionDate IS NOT NULL AND last_activity.StopExecutionDate IS NULL THEN 'Executing' ELSE 'Idle' END,
                                [Enabled]           = jobs.[enabled]
                        FROM sysjobs AS jobs WITH(NOLOCK)
                        OUTER APPLY (
                            SELECT		TOP 1
				                        [StartExecutionDate]    = start_execution_date,
				                        [StopExecutionDate]     = stop_execution_date,
				                        [NextExecution]         = next_scheduled_run_date
                            FROM		sysjobactivity WITH(NOLOCK)
                            WHERE		job_id = jobs.job_id
                            ORDER BY	[session_id] DESC) AS last_activity
                        WHERE		1 = 1
                        $WHERE$
                    )
                    SELECT      *
                    FROM        cte_jobs
                    ORDER BY    [Enabled] DESC,
                                CASE [Status] WHEN 'Executing' THEN 0 ELSE 1 END ASC,
                                [Name] ASC
                ";

            string whereClause = string.Empty;
            if (!string.IsNullOrWhiteSpace(filter.Name))
                whereClause += $" AND jobs.[Name] LIKE '%{filter.Name}%'";

            if (filter.EnabledYes && !filter.EnabledNo)
                whereClause += $" AND jobs.[Enabled] = 1";

            if (!filter.EnabledYes && filter.EnabledNo)
                whereClause += $" AND jobs.[Enabled] = 0";

            if (!filter.EnabledYes && !filter.EnabledNo)
                whereClause += $" AND 1=2";

            sql = sql.Replace("$WHERE$", whereClause);
            List<JobMonitor> ret = dbService.Query<JobMonitor>(sql);
            return ret;
        }

        public static void StartJob(string server, string jobId)
        {
            throw new NotImplementedException("StartJob method is not implemented yet.");
        }

        public static void StopJob(string server, string jobId)
        {
            throw new NotImplementedException("StopJob method is not implemented yet.");
        }
    }
}
