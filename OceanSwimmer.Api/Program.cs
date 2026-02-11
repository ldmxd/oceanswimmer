using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles();   // 👈 THIS is the key line



var connStr = Environment.GetEnvironmentVariable("OCEANSWIMMER_SQL");

if (string.IsNullOrEmpty(connStr))
{
    throw new Exception("Missing OCEANSWIMMER_SQL connection string");
}



app.MapGet("/swims/search", async (
    string? forename,
    string? surname,
    string? race,
    int ? raceId,

    int page = 1,
    int pageSize = 250) =>

{
    page = Math.Max(page, 1);
    if (raceId != null)
    {
        // Race search: allow big result sets
        pageSize = Math.Clamp(pageSize, 1, 10_000);
    }
    else
    {
        // Name search: keep it tight
        pageSize = Math.Clamp(pageSize, 1, 250);
    }

    int offset = (page - 1) * pageSize;

    var results = new List<object>();

    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();

    var cmd = new SqlCommand("""
        SELECT
            raceid,                   -- 0 
            RaceName,                 -- 1
            Distance,                 -- 2
            RaceTime,                 -- 3
            Forename,                 -- 4
            Surname,                  -- 5
            FullName,                 -- 6
            OverallPosition,          -- 7
            OverallCompetitors,       -- 8
            OverallPercentile,        -- 9
            GenderPosition,           -- 10
            GenderCompetitors,        -- 11
            GenderPercentile,         -- 12
            CategoryPosition,         -- 13
            CategoryCompetitors,      -- 14
            CategoryPercentile        -- 15
        FROM dbo.vw_OceanSwims_Search
        WHERE
            (@surname  IS NULL OR Surname_Search  LIKE @surname COLLATE Latin1_General_CI_AI)
            AND (@forename IS NULL OR Forename_Search LIKE @forename COLLATE Latin1_General_CI_AI)
            AND (@raceId IS NULL OR raceid = @raceId)
    
        ORDER BY RaceName, OverallPosition
        OFFSET @offset ROWS
        FETCH NEXT @pageSize ROWS ONLY;
    """, conn);

    cmd.Parameters.AddWithValue("@surname",
    string.IsNullOrWhiteSpace(surname)
        ? DBNull.Value
        : $"%{surname.Trim().ToUpper()}%");

    cmd.Parameters.AddWithValue("@forename",
        string.IsNullOrWhiteSpace(forename) ? DBNull.Value : $"%{forename}%");

    cmd.Parameters.AddWithValue("@race",
    string.IsNullOrWhiteSpace(race) ? DBNull.Value : race);

    cmd.Parameters.AddWithValue("@offset", offset);
    cmd.Parameters.AddWithValue("@pageSize", pageSize);

    if (raceId.HasValue)
    {
        cmd.Parameters.AddWithValue("@raceId", raceId.Value);
    }
    else
    {
        cmd.Parameters.AddWithValue("@raceId", DBNull.Value);
    }

    using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        results.Add(new
        {
            raceId = rdr.GetInt32(rdr.GetOrdinal("raceid")),
            raceName = rdr["RaceName"] as string,
            distance = rdr["Distance"] as decimal?,
            raceTime = rdr["RaceTime"] as TimeSpan?,
            forename = rdr["Forename"] as string,
            surname = rdr["Surname"] as string,
            fullName = rdr["FullName"] as string,
            overallPosition = rdr["OverallPosition"] as int?,
            overallCompetitors = rdr["OverallCompetitors"] as int?,
            overallPercentile = rdr["OverallPercentile"] as decimal?,
            genderPosition = rdr["GenderPosition"] as int?,
            genderCompetitors = rdr["GenderCompetitors"] as int?,
            genderPercentile = rdr["GenderPercentile"] as decimal?,
            categoryPosition = rdr["CategoryPosition"] as int?,
            categoryCompetitors = rdr["CategoryCompetitors"] as int?,
            categoryPercentile = rdr["CategoryPercentile"] as decimal?
        });



    }

    return Results.Ok(new
    {
        page,
        pageSize,
        count = results.Count,
        results
    });
});

app.MapGet("/races", async (string? q) =>
{
    var results = new List<object>();

    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();

    var sql = @"
        SELECT
            raceid,
            RaceName,
            RaceDescription,
            Distance,
            COUNT(*) AS ResultCount
        FROM dbo.vw_OceanSwims_Search
        WHERE (@q IS NULL OR RaceDescription LIKE '%' + @q + '%')
        GROUP BY
            raceid,
            RaceName,
            RaceDescription,
            Distance
        ORDER BY
            RaceDescription";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@q", (object?)q ?? DBNull.Value);

    using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        results.Add(new
        {
            raceId = (int)rdr["raceid"],
            raceName = rdr["RaceName"] as string,
            raceDescription = rdr["RaceDescription"] as string,
            distance = rdr["Distance"] as decimal?,
            resultCount = (int)rdr["ResultCount"]
        });
    }

    return Results.Ok(results);
});


app.Run();
