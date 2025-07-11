using quick_sql.Model;

namespace quick_sql.Service
{
    internal static class ObjectSearchService
    {
        public static List<ObjectSearch> Search(ObjectSearchFilter filter)
        {
            using DbService dbService = new(filter.Server, "master");
            string sql =
                @"  
                    IF OBJECT_ID('tempdb..#results') IS NOT NULL DROP TABLE #results
                    CREATE TABLE #results ([Database] VARCHAR(500), [Type] VARCHAR(100), [Name] VARCHAR(MAX), [Code] VARCHAR(MAX))

                    DECLARE @CURSOR_DB_NAME VARCHAR(500)
                    DECLARE DB_CURSOR CURSOR 
                    FOR
	                    SELECT		[name] 
	                    FROM		SYS.DATABASES 
	                    WHERE		STATE_DESC = 'ONLINE'
	                    $DB_FILTER$
	                    ORDER BY	[name]
                    OPEN DB_CURSOR  
	                    FETCH NEXT FROM DB_CURSOR INTO @CURSOR_DB_NAME
  
	                    WHILE @@FETCH_STATUS = 0  
	                    BEGIN 
		                    DECLARE @TSQL NVARCHAR(MAX)  = '
				                    USE [@CURSOR_DB_NAME]; 
				                    INSERT INTO #RESULTS
				                    SELECT		DISTINCT
							                    ''@CURSOR_DB_NAME'',
							                    CASE O.TYPE_DESC
                                                    WHEN ''SQL_TRIGGER''                        THEN ''Trigger''
                                                    WHEN ''SQL_SCALAR_FUNCTION''                THEN ''Function (Scalar)''
                                                    WHEN ''SQL_TABLE_VALUED_FUNCTION''          THEN ''Function (Table Valued)''
                                                    WHEN ''SQL_INLINE_TABLE_VALUED_FUNCTION''   THEN ''Function (Inline Table Valued)''
                                                    WHEN ''SQL_STORED_PROCEDURE''               THEN ''Stored Procedure''
                                                    WHEN ''VIEW''                               THEN ''View''
                                                    ELSE O.TYPE_DESC
                                                END,
							                    O.NAME,
							                    M.DEFINITION
				                    FROM		[@CURSOR_DB_NAME].SYS.SQL_MODULES	AS M
				                    INNER JOIN	SYS.OBJECTS							AS O ON M.OBJECT_ID = O.OBJECT_ID AND O.IS_MS_SHIPPED = 0
				                    WHERE		$WHERE$
		                    ';  

		                    SET @TSQL = REPLACE(@TSQL, '@CURSOR_DB_NAME', @CURSOR_DB_NAME)
		                    EXEC SP_EXECUTESQL @TSQL
	
		                    FETCH NEXT FROM DB_CURSOR   
		                    INTO @CURSOR_DB_NAME
	                    END

                    CLOSE		DB_CURSOR
                    DEALLOCATE	DB_CURSOR

                    SELECT		*
                    FROM		#RESULTS
                    ORDER BY	[Database], [Type], [Name]
                ";

            string dbFilter = string.Empty;
            if (!string.IsNullOrWhiteSpace(filter.Database))
                dbFilter += $" AND [name] LIKE '{filter.Database}'";

            string whereClause =
                filter.SearchInName && filter.SearchInCode ?
                    @$"O.NAME LIKE ''%{filter.Term}%'' OR M.DEFINITION LIKE ''%{filter.Term}%'' ESCAPE ''\''" :
                filter.SearchInName ?
                    @$"O.NAME LIKE ''%{filter.Term}%''" :
                filter.SearchInCode ?
                    @$"M.DEFINITION LIKE ''%{filter.Term}%'' ESCAPE ''\''" :
                    "1 = 0";

            sql = sql.Replace("$WHERE$", whereClause);
            sql = sql.Replace("$DB_FILTER$", dbFilter);
            List<ObjectSearch> ret = dbService.Query<ObjectSearch>(sql);

            return ret;
        }
    }
}
