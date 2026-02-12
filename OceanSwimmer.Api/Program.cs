using Microsoft.Data.SqlClient;
using System.Globalization;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles();   // 👈 THIS is the key line



var connStr = Environment.GetEnvironmentVariable("OCEANSWIMMER_SQL");

if (string.IsNullOrEmpty(connStr))
{
    throw new Exception("Missing OCEANSWIMMER_SQL connection string");
}


app.UseDefaultFiles();
app.UseStaticFiles();



app.MapGet("/swims/search", async (
    string? forename,
    string? surname,
    string? race,
    int ? raceId,
    string ? category,
    string? gender,

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
            raceid,
            RaceDate,
            RaceName,
            Distance,
            RaceTime,
            Category,
            Sex,
            Forename,
            Surname,
            FullName,
            OverallPosition,
            OverallCompetitors,
            OverallPercentile,
            GenderPosition,
            GenderCompetitors,
            GenderPercentile,
            CategoryPosition,
            CategoryCompetitors,
            CategoryPercentile
        FROM dbo.vw_OceanSwims_Search
        WHERE
            (@surname  IS NULL OR Surname_Search  LIKE @surname COLLATE Latin1_General_CI_AI)
            AND (@forename IS NULL OR Forename_Search LIKE @forename COLLATE Latin1_General_CI_AI)
            AND (@raceId IS NULL OR raceid = @raceId)
            AND (@category IS NULL OR Category = @category)
            AND (@gender IS NULL OR Sex = @gender)
    
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

    cmd.Parameters.AddWithValue("@gender", (object?)gender ?? DBNull.Value);

    cmd.Parameters.AddWithValue("@category",
    string.IsNullOrWhiteSpace(category) ? DBNull.Value : category);


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

    // 🔹 Cache ordinals once
    var ordRaceId = rdr.GetOrdinal("raceid");
    var ordRaceDate = rdr.GetOrdinal("RaceDate");
    var ordRaceName = rdr.GetOrdinal("RaceName");
    var ordDistance = rdr.GetOrdinal("Distance");
    var ordRaceTime = rdr.GetOrdinal("RaceTime");
    var ordCategory = rdr.GetOrdinal("Category");
    var ordSex = rdr.GetOrdinal("Sex");
    var ordForename = rdr.GetOrdinal("Forename");
    var ordSurname = rdr.GetOrdinal("Surname");
    var ordFullName = rdr.GetOrdinal("FullName");
    var ordOverallPosition = rdr.GetOrdinal("OverallPosition");
    var ordOverallCompetitors = rdr.GetOrdinal("OverallCompetitors");
    var ordOverallPercentile = rdr.GetOrdinal("OverallPercentile");
    var ordGenderPosition = rdr.GetOrdinal("GenderPosition");
    var ordGenderCompetitors = rdr.GetOrdinal("GenderCompetitors");
    var ordGenderPercentile = rdr.GetOrdinal("GenderPercentile");
    var ordCategoryPosition = rdr.GetOrdinal("CategoryPosition");
    var ordCategoryCompetitors = rdr.GetOrdinal("CategoryCompetitors");
    var ordCategoryPercentile = rdr.GetOrdinal("CategoryPercentile");

    while (await rdr.ReadAsync())
    {
        results.Add(new
        {
            raceId = rdr.GetInt32(ordRaceId),

            raceDate = rdr.IsDBNull(ordRaceDate)
                ? (DateTime?)null
                : rdr.GetDateTime(ordRaceDate),


            raceName = rdr.IsDBNull(ordRaceName)
                ? null
                : rdr.GetString(ordRaceName),

            distance = rdr.IsDBNull(ordDistance)
                ? (decimal?)null
                : rdr.GetDecimal(ordDistance),

            raceTime = rdr.IsDBNull(ordRaceTime)
                ? (TimeSpan?)null
                : rdr.GetTimeSpan(ordRaceTime),

            category = rdr.IsDBNull(ordCategory)
                ? null
                : rdr.GetString(ordCategory),

            gender = rdr.IsDBNull(ordSex)
                ? null
                : rdr.GetString(ordSex).ToUpper().StartsWith("M")
                    ? "Male"
                    : rdr.GetString(ordSex).ToUpper().StartsWith("F")
                        ? "Female"
                        : rdr.GetString(ordSex),

            forename = rdr.IsDBNull(ordForename)
                ? null
                : rdr.GetString(ordForename),

            surname = rdr.IsDBNull(ordSurname)
                ? null
                : rdr.GetString(ordSurname),

            fullName = rdr.IsDBNull(ordFullName)
                ? null
                : rdr.GetString(ordFullName),

            overallPosition = rdr.IsDBNull(ordOverallPosition)
                ? (int?)null
                : rdr.GetInt32(ordOverallPosition),

            overallCompetitors = rdr.IsDBNull(ordOverallCompetitors)
                ? (int?)null
                : rdr.GetInt32(ordOverallCompetitors),

            overallPercentile = rdr.IsDBNull(ordOverallPercentile)
                ? (decimal?)null
                : rdr.GetDecimal(ordOverallPercentile),

            genderPosition = rdr.IsDBNull(ordGenderPosition)
                ? (int?)null
                : rdr.GetInt32(ordGenderPosition),

            genderCompetitors = rdr.IsDBNull(ordGenderCompetitors)
                ? (int?)null
                : rdr.GetInt32(ordGenderCompetitors),

            genderPercentile = rdr.IsDBNull(ordGenderPercentile)
                ? (decimal?)null
                : rdr.GetDecimal(ordGenderPercentile),

            categoryPosition = rdr.IsDBNull(ordCategoryPosition)
                ? (int?)null
                : rdr.GetInt32(ordCategoryPosition),

            categoryCompetitors = rdr.IsDBNull(ordCategoryCompetitors)
                ? (int?)null
                : rdr.GetInt32(ordCategoryCompetitors),

            categoryPercentile = rdr.IsDBNull(ordCategoryPercentile)
                ? (decimal?)null
                : rdr.GetDecimal(ordCategoryPercentile)
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
            RaceDate,
            Distance,
            COUNT(*) AS ResultCount
        FROM dbo.vw_OceanSwims_Search
        WHERE (@q IS NULL OR RaceName LIKE '%' + @q + '%')
        GROUP BY
            raceid,
            RaceName,
            RaceDate,
            Distance
        ORDER BY
            RaceDate DESC, RaceName";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@q", (object?)q ?? DBNull.Value);

    using var rdr = await cmd.ExecuteReaderAsync();
    var ordRaceId = rdr.GetOrdinal("raceid");
    var ordRaceName = rdr.GetOrdinal("RaceName");
    var ordRaceDate = rdr.GetOrdinal("RaceDate");
    var ordDistance = rdr.GetOrdinal("Distance");
    var ordResultCount = rdr.GetOrdinal("ResultCount");

    while (await rdr.ReadAsync())
    {
        results.Add(new
        {
            raceId = rdr.GetInt32(ordRaceId),

            raceName = rdr.IsDBNull(ordRaceName)
                ? null
                : rdr.GetString(ordRaceName),

            raceDate = rdr.IsDBNull(ordRaceDate)
                ? null
                : rdr.GetDateTime(ordRaceDate)
                      .ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture),

            distance = rdr.IsDBNull(ordDistance)
                ? (decimal?)null
                : rdr.GetDecimal(ordDistance),

            resultCount = rdr.GetInt32(ordResultCount)
        });
    }


    return Results.Ok(results);
});


app.Run();
