using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using OceanSwimmer.Api.Helpers;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Globalization;
using System.Security.Claims;
using System.Text;


// ✅ Read connection string first — needed in Google OAuth closure below
var connStr = Environment.GetEnvironmentVariable("OCEANSWIMMER_SQL");
if (string.IsNullOrEmpty(connStr))
    throw new Exception("Missing OCEANSWIMMER_SQL connection string");


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("keys"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddGoogle("Google", options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.SignInScheme = "Cookies";
    options.CallbackPath = "/signin-google";

    // ✅ After Google authenticates, upsert the user in our Users table
    //    and attach their UserId as a claim so all endpoints can read it.
    options.Events.OnCreatingTicket = async ctx =>
    {
        var email = ctx.Identity?.FindFirst(ClaimTypes.Email)?.Value;
        var providerId = ctx.Identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (email == null || providerId == null) return;

        using var conn = new SqlConnection(connStr);

        // Find existing user by Google provider ID
        var userId = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT UserId FROM auth.Users WHERE AuthProvider = 'Google' AND ProviderId = @providerId",
            new { providerId });

        // Email match — already registered via Facebook or email/password
        if (userId == null && email != null)
            userId = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT UserId FROM auth.Users WHERE Email = @email",
                new { email });

        // First-time login — create the user (Google users are already email-verified)
        if (userId == null)
        {
            userId = await conn.QuerySingleAsync<int>(@"
                INSERT INTO auth.Users (Email, AuthProvider, ProviderId, CreatedAt, EmailVerified)
                OUTPUT INSERTED.UserId
                VALUES (@email, 'Google', @providerId, GETUTCDATE(), 1)",
                new { email, providerId });
        }

        // Attach userId to the cookie so endpoints can read it cheaply
        ctx.Identity!.AddClaim(new Claim("userId", userId.ToString()!));
    };
})
.AddFacebook("Facebook", options =>
{
    options.AppId     = builder.Configuration["Authentication:Facebook:AppId"]!;
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
    options.SignInScheme  = "Cookies";
    options.CallbackPath  = "/signin-facebook";
    options.Scope.Add("email");

    options.Events.OnCreatingTicket = async ctx =>
    {
        var email      = ctx.Identity?.FindFirst(ClaimTypes.Email)?.Value;
        var providerId = ctx.Identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (providerId == null) return;

        using var conn = new SqlConnection(connStr);

        var userId = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT UserId FROM auth.Users WHERE AuthProvider = 'Facebook' AND ProviderId = @providerId",
            new { providerId });

        // Email match — already registered via Google or email/password
        if (userId == null && email != null)
            userId = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT UserId FROM auth.Users WHERE Email = @email",
                new { email });

        if (userId == null)
        {
            userId = await conn.QuerySingleAsync<int>(@"
                INSERT INTO auth.Users (Email, AuthProvider, ProviderId, CreatedAt, EmailVerified)
                OUTPUT INSERTED.UserId
                VALUES (@email, 'Facebook', @providerId, GETUTCDATE(), 1)",
                new { email, providerId });
        }

        ctx.Identity!.AddClaim(new Claim("userId", userId.ToString()!));
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


// ---------------------------------------------------------------------------
// AUTH ENDPOINTS
// ---------------------------------------------------------------------------

// Kick off Google OAuth flow
app.MapGet("/login/google", async (HttpContext context) =>
{
    await context.ChallengeAsync("Google", new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

// Who am I? — called by the frontend on page load
app.MapGet("/auth/me", async (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var userIdStr = ctx.User.FindFirst("userId")?.Value;

    string? swimmerForename = null, swimmerSurname = null;
    if (userIdStr != null)
    {
        var userId = int.Parse(userIdStr);
        using var conn = new SqlConnection(connStr);

        var names = await conn.QueryFirstOrDefaultAsync(
            "SELECT SwimmerForename, SwimmerSurname FROM auth.Users WHERE UserId = @userId",
            new { userId });
        swimmerForename = (string?)names?.SwimmerForename;
        swimmerSurname  = (string?)names?.SwimmerSurname;

        // Name not stored yet — infer it from an existing claimed result and save it
        if (swimmerForename == null && swimmerSurname == null)
        {
            var inferred = await conn.QueryFirstOrDefaultAsync(@"
                SELECT TOP 1 o.Forename, o.Surname
                FROM auth.AthleteResults ar
                JOIN dbo.OceanSwims o ON o.oceanswimsid = ar.OceanSwimsId
                WHERE ar.UserId = @userId
                ORDER BY ar.ClaimedAt DESC",
                new { userId });

            if (inferred != null)
            {
                swimmerForename = (string?)inferred.Forename;
                swimmerSurname  = (string?)inferred.Surname;

                // Persist so we don't have to infer again
                await conn.ExecuteAsync(
                    "UPDATE auth.Users SET SwimmerForename = @f, SwimmerSurname = @s WHERE UserId = @userId",
                    new { f = swimmerForename, s = swimmerSurname, userId });
            }
        }
    }

    return Results.Ok(new
    {
        userId          = userIdStr,
        email           = ctx.User.FindFirst(ClaimTypes.Email)?.Value,
        name            = ctx.User.FindFirst(ClaimTypes.Name)?.Value,
        picture         = ctx.User.FindFirst("urn:google:picture")?.Value,
        swimmerForename,
        swimmerSurname
    });
});

// Sign out
app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync("Cookies");
    return Results.Ok();
});

// Kick off Facebook OAuth flow
app.MapGet("/login/facebook", async (HttpContext context) =>
{
    await context.ChallengeAsync("Facebook", new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

// Register with email + password
app.MapPost("/auth/register", async (RegisterRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Email and password are required." });

    var email = req.Email.Trim().ToLower();

    if (!email.Contains('@') || !email.Contains('.'))
        return Results.BadRequest(new { error = "Please enter a valid email address." });

    if (req.Password.Length < 8)
        return Results.BadRequest(new { error = "Password must be at least 8 characters." });

    using var conn = new SqlConnection(connStr);

    var existing = await conn.QueryFirstOrDefaultAsync<int?>(
        "SELECT UserId FROM auth.Users WHERE Email = @email",
        new { email });

    if (existing != null)
        return Results.Conflict(new { error = "An account with that email already exists." });

    var hash    = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var token   = Guid.NewGuid().ToString("N");
    var expiry  = DateTime.UtcNow.AddHours(24);

    await conn.ExecuteAsync(@"
        INSERT INTO auth.Users (Email, AuthProvider, ProviderId, CreatedAt, PasswordHash, EmailVerified, VerificationToken, VerificationTokenExpiry)
        VALUES (@email, 'Local', @email, GETUTCDATE(), @hash, 0, @token, @expiry)",
        new { email, hash, token, expiry });

    await SendVerificationEmailAsync(email, token);

    return Results.Ok(new { message = "Account created! Please check your email to verify your address before signing in." });
});

// Verify email address via token link
app.MapGet("/auth/verify-email", async (string token, HttpContext ctx) =>
{
    using var conn = new SqlConnection(connStr);

    var user = await conn.QueryFirstOrDefaultAsync(@"
        SELECT UserId, Email FROM auth.Users
        WHERE VerificationToken = @token
          AND VerificationTokenExpiry > GETUTCDATE()
          AND EmailVerified = 0",
        new { token });

    if (user == null)
        return Results.Redirect("/login.html?error=invalid-token");

    int userId = (int)user.UserId;

    await conn.ExecuteAsync(@"
        UPDATE auth.Users
        SET EmailVerified = 1, VerificationToken = NULL, VerificationTokenExpiry = NULL
        WHERE UserId = @userId",
        new { userId });

    // Sign them straight in
    var claims = new List<Claim>
    {
        new Claim("userId", userId.ToString()),
        new Claim(ClaimTypes.Email, (string)user.Email)
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    await ctx.SignInAsync("Cookies", principal);

    return Results.Redirect("/?verified=1");
});

// Sign in with email + password
app.MapPost("/auth/login-password", async (LoginRequest req, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Email and password are required." });

    var email = req.Email.Trim().ToLower();

    using var conn = new SqlConnection(connStr);

    var user = await conn.QueryFirstOrDefaultAsync(@"
        SELECT UserId, Email, PasswordHash, EmailVerified
        FROM auth.Users
        WHERE Email = @email AND AuthProvider = 'Local'",
        new { email });

    // Deliberate vague error — don't reveal whether email exists
    if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, (string)(user.PasswordHash ?? "")))
        return Results.BadRequest(new { error = "Invalid email or password." });

    if (!(bool)user.EmailVerified)
        return Results.BadRequest(new { error = "Please verify your email address before signing in." });

    int userId = (int)user.UserId;

    var claims = new List<Claim>
    {
        new Claim("userId", userId.ToString()),
        new Claim(ClaimTypes.Email, (string)user.Email)
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    await ctx.SignInAsync("Cookies", principal);

    return Results.Ok(new { message = "Signed in." });
});


// ---------------------------------------------------------------------------
// CLAIM ENDPOINTS  (requires login)
// ---------------------------------------------------------------------------

// Claim a swim result
app.MapPost("/swims/{oceanswimsId}/claim", async (int oceanswimsId, HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);

    // Swim must exist — also grab its raceId
    var swim = await conn.QueryFirstOrDefaultAsync(
        "SELECT oceanswimsid, raceid FROM dbo.OceanSwims WHERE oceanswimsid = @oceanswimsId",
        new { oceanswimsId });

    if (swim == null) return Results.NotFound();

    int raceId = (int)swim.raceid;

    // Set swimmer identity on first ever claim (used for fuzzy search / Results to Claim)
    var hasIdentity = await conn.QueryFirstOrDefaultAsync<bool?>(
        "SELECT CAST(1 AS bit) FROM auth.Users WHERE UserId = @userId AND SwimmerForename IS NOT NULL",
        new { userId });
    if (hasIdentity != true)
    {
        var swimFull = await conn.QueryFirstOrDefaultAsync(
            "SELECT Forename, Surname FROM dbo.OceanSwims WHERE oceanswimsid = @oceanswimsId",
            new { oceanswimsId });
        await conn.ExecuteAsync(
            "UPDATE auth.Users SET SwimmerForename = @f, SwimmerSurname = @s WHERE UserId = @userId",
            new { f = (string?)swimFull?.Forename ?? "", s = (string?)swimFull?.Surname ?? "", userId });
    }

    // One claim per race — check the user hasn't already claimed a different result in this race
    var existingForRace = await conn.QueryFirstOrDefaultAsync<int?>(
        @"SELECT ar.OceanSwimsId FROM auth.AthleteResults ar
          JOIN dbo.OceanSwims o ON o.oceanswimsid = ar.OceanSwimsId
          WHERE ar.UserId = @userId AND o.raceid = @raceId AND ar.OceanSwimsId != @oceanswimsId",
        new { userId, raceId, oceanswimsId });

    if (existingForRace != null)
        return Results.Conflict(new { error = "You have already claimed a result for this race." });

    // Idempotent insert
    await conn.ExecuteAsync(@"
        IF NOT EXISTS (
            SELECT 1 FROM auth.AthleteResults
            WHERE UserId = @userId AND OceanSwimsId = @oceanswimsId
        )
        INSERT INTO auth.AthleteResults (UserId, OceanSwimsId, ClaimedAt)
        VALUES (@userId, @oceanswimsId, GETUTCDATE())",
        new { userId, oceanswimsId });

    return Results.Ok();
});

// Claim all swims in one go — body: { "ids": [1,2,3,...] }
// One result per race is enforced: if the user already has a claim for a race, all
// supplied IDs for that race are skipped. Where multiple supplied IDs belong to the
// same unclaimed race the lowest oceanswimsid wins.
app.MapPost("/swims/claim-all", async (ClaimAllRequest req, HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    if (req.Ids == null || req.Ids.Count == 0) return Results.BadRequest("No IDs supplied");

    var ids = req.Ids.Distinct().Take(2000).ToList();
    var idsJson = System.Text.Json.JsonSerializer.Serialize(ids);

    using var conn = new SqlConnection(connStr);

    // Bulk insert — picks one result per race (lowest oceanswimsid), skips races
    // where the user already has a claim.
    var inserted = await conn.ExecuteAsync(@"
        WITH candidates AS (
            SELECT o.oceanswimsid,
                   ROW_NUMBER() OVER (PARTITION BY o.raceid ORDER BY o.oceanswimsid) AS rn
            FROM OPENJSON(@idsJson) j
            JOIN dbo.OceanSwims o ON o.oceanswimsid = CAST(j.value AS INT)
            WHERE NOT EXISTS (
                SELECT 1 FROM auth.AthleteResults ar
                JOIN dbo.OceanSwims o2 ON o2.oceanswimsid = ar.OceanSwimsId
                WHERE ar.UserId = @userId AND o2.raceid = o.raceid
            )
        )
        INSERT INTO auth.AthleteResults (UserId, OceanSwimsId, ClaimedAt)
        SELECT @userId, oceanswimsid, GETUTCDATE()
        FROM candidates
        WHERE rn = 1
          AND NOT EXISTS (
              SELECT 1 FROM auth.AthleteResults ar
              WHERE ar.UserId = @userId AND ar.OceanSwimsId = candidates.oceanswimsid
          )",
        new { userId, idsJson });

    return Results.Ok(new { claimed = inserted });
});

// Manually claim a swim (e.g. data entry typo in the original result name)
// Flagged as IsManualClaim = 1 for audit purposes
app.MapPost("/swims/{oceanswimsId}/claim-manual", async (int oceanswimsId, HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);

    // Swim must exist — also grab its raceId
    var swim = await conn.QueryFirstOrDefaultAsync(
        "SELECT oceanswimsid, raceid FROM dbo.OceanSwims WHERE oceanswimsid = @oceanswimsId",
        new { oceanswimsId });
    if (swim == null) return Results.NotFound();

    int raceId = (int)swim.raceid;

    // One claim per race — check the user hasn't already claimed a different result in this race
    var existingForRace = await conn.QueryFirstOrDefaultAsync<int?>(
        @"SELECT ar.OceanSwimsId FROM auth.AthleteResults ar
          JOIN dbo.OceanSwims o ON o.oceanswimsid = ar.OceanSwimsId
          WHERE ar.UserId = @userId AND o.raceid = @raceId AND ar.OceanSwimsId != @oceanswimsId",
        new { userId, raceId, oceanswimsId });

    if (existingForRace != null)
        return Results.Conflict(new { error = "You have already claimed a result for this race." });

    await conn.ExecuteAsync(@"
        IF NOT EXISTS (
            SELECT 1 FROM auth.AthleteResults
            WHERE UserId = @userId AND OceanSwimsId = @oceanswimsId
        )
        INSERT INTO auth.AthleteResults (UserId, OceanSwimsId, ClaimedAt, IsManualClaim)
        VALUES (@userId, @oceanswimsId, GETUTCDATE(), 1)",
        new { userId, oceanswimsId });

    return Results.Ok();
});

// Mark / unmark an A Race
// ⚠️  Requires: ALTER TABLE auth.AthleteResults ADD IsARace bit NOT NULL DEFAULT 0
app.MapPost("/swims/{oceanswimsId}/a-race", async (int oceanswimsId, HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);
    await conn.ExecuteAsync(
        "UPDATE auth.AthleteResults SET IsARace = 1 WHERE UserId = @userId AND OceanSwimsId = @oceanswimsId",
        new { userId, oceanswimsId });

    return Results.Ok();
});

app.MapDelete("/swims/{oceanswimsId}/a-race", async (int oceanswimsId, HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);
    await conn.ExecuteAsync(
        "UPDATE auth.AthleteResults SET IsARace = 0 WHERE UserId = @userId AND OceanSwimsId = @oceanswimsId",
        new { userId, oceanswimsId });

    return Results.Ok();
});

// Unclaim a swim result
app.MapDelete("/swims/{oceanswimsId}/claim", async (int oceanswimsId, HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);

    await conn.ExecuteAsync(
        "DELETE FROM auth.AthleteResults WHERE UserId = @userId AND OceanSwimsId = @oceanswimsId",
        new { userId, oceanswimsId });

    return Results.Ok();
});


// ---------------------------------------------------------------------------
// ATHLETE PAGE
// ---------------------------------------------------------------------------

// All claimed swims for the logged-in athlete, with full stats
app.MapGet("/athlete/swims", async (HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);

    var swims = await conn.QueryAsync(@"
        SELECT
            o.oceanswimsid,
            o.raceid,
            o.RaceDate,
            o.RaceName,
            o.Distance,
            o.RaceTime,
            o.Category,
            o.Gender,
            o.Forename,
            o.Surname,
            o.OverallPosition,
            o.OverallCompetitors,
            o.OverallPercentile,
            o.GenderPosition,
            o.GenderCompetitors,
            o.GenderPercentile,
            o.CategoryPosition,
            o.CategoryCompetitors,
            o.CategoryPercentile,
            ar.ClaimedAt,
            ar.IsARace
        FROM auth.AthleteResults ar
        JOIN dbo.vw_OceanSwims_Search o ON o.oceanswimsid = ar.OceanSwimsId
        WHERE ar.UserId = @userId
        ORDER BY o.RaceDate DESC",
        new { userId });

    return Results.Ok(swims);
});


// ---------------------------------------------------------------------------
// EXISTING ENDPOINTS (unchanged except oceanswimsId added to search)
// ---------------------------------------------------------------------------

app.MapPost("/api/feedback", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();

    var name    = form["FeedbackName"];
    var email   = form["FeedbackEmail"];
    var type    = form["RequestType"];
    var message = form["FeedbackMessage"];
    var pageUrl = form["PageUrl"];

    using var conn = new SqlConnection(connStr);

    await conn.ExecuteAsync(@"
        INSERT INTO dbo.Feedback
        (FeedbackName, FeedbackEmail, RequestType, FeedbackMessage, PageUrl)
        VALUES
        (@name, @email, @type, @message, @pageUrl)",
        new { name, email, type, message, pageUrl });

    return Results.Redirect("/feedback-thanks.html");
});

app.MapGet("/api/race-count", async () =>
{
    using var conn = new SqlConnection(connStr);

    var count = await conn.QuerySingleAsync<int>(
        "SELECT COUNT(DISTINCT RaceId) FROM dbo.OceanSwims"
    );

    return Results.Ok(count);
});

app.MapGet("/sitemap.xml", async () =>
{
    using var conn = new SqlConnection(connStr);

    var races = await conn.QueryAsync<(int RaceId, string RaceName)>(
        "SELECT DISTINCT raceid, RaceName FROM dbo.vw_OceanSwims_Search"
    );

    var sb = new StringBuilder();

    sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
    sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

    sb.AppendLine(@"
  <url>
    <loc>https://oceanswimmer.com.au/</loc>
  </url>");

    foreach (var r in races)
    {
        var slug = SlugHelper.GenerateSlug(r.RaceName);
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>https://oceanswimmer.com.au/results/{slug}-{r.RaceId}</loc>");
        sb.AppendLine("  </url>");
    }

    sb.AppendLine("</urlset>");

    return Results.Text(sb.ToString(), "application/xml");
});


app.MapGet("/swims/search", async (
    string? forename,
    string? surname,
    string? race,
    int?    raceId,
    string? category,
    string? gender,
    int page     = 1,
    int pageSize = 250) =>
{
    page = Math.Max(page, 1);
    if (raceId != null)
        pageSize = Math.Clamp(pageSize, 1, 10_000);
    else
        pageSize = Math.Clamp(pageSize, 1, 250);

    int offset = (page - 1) * pageSize;

    var results = new List<object>();

    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();

    var cmd = new SqlCommand("""
        SELECT
            oceanswimsid,
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
            (@surname  IS NULL OR Surname_Search  LIKE @surname  COLLATE Latin1_General_CI_AI)
            AND (@forename IS NULL OR Forename_Search LIKE @forename COLLATE Latin1_General_CI_AI)
            AND (@raceId   IS NULL OR raceid = @raceId)
            AND (@category IS NULL OR Category = @category)
            AND (@gender   IS NULL OR Sex = @gender)
        ORDER BY RaceName, OverallPosition
        OFFSET @offset ROWS
        FETCH NEXT @pageSize ROWS ONLY;
        """, conn);

    cmd.Parameters.AddWithValue("@surname",
        string.IsNullOrWhiteSpace(surname) ? DBNull.Value : $"%{surname.Trim().ToUpper()}%");

    cmd.Parameters.AddWithValue("@forename",
        string.IsNullOrWhiteSpace(forename) ? DBNull.Value : $"%{forename}%");

    cmd.Parameters.AddWithValue("@race",
        string.IsNullOrWhiteSpace(race) ? DBNull.Value : race);

    cmd.Parameters.AddWithValue("@gender",   (object?)gender   ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@category",
        string.IsNullOrWhiteSpace(category) ? DBNull.Value : category);

    cmd.Parameters.AddWithValue("@offset",   offset);
    cmd.Parameters.AddWithValue("@pageSize", pageSize);
    cmd.Parameters.AddWithValue("@raceId",   raceId.HasValue ? raceId.Value : DBNull.Value);

    using var rdr = await cmd.ExecuteReaderAsync();

    // Cache ordinals once
    var ordOceanSwimsId       = rdr.GetOrdinal("oceanswimsid");
    var ordRaceId             = rdr.GetOrdinal("raceid");
    var ordRaceDate           = rdr.GetOrdinal("RaceDate");
    var ordRaceName           = rdr.GetOrdinal("RaceName");
    var ordDistance           = rdr.GetOrdinal("Distance");
    var ordRaceTime           = rdr.GetOrdinal("RaceTime");
    var ordCategory           = rdr.GetOrdinal("Category");
    var ordSex                = rdr.GetOrdinal("Sex");
    var ordForename           = rdr.GetOrdinal("Forename");
    var ordSurname            = rdr.GetOrdinal("Surname");
    var ordFullName           = rdr.GetOrdinal("FullName");
    var ordOverallPosition    = rdr.GetOrdinal("OverallPosition");
    var ordOverallCompetitors = rdr.GetOrdinal("OverallCompetitors");
    var ordOverallPercentile  = rdr.GetOrdinal("OverallPercentile");
    var ordGenderPosition     = rdr.GetOrdinal("GenderPosition");
    var ordGenderCompetitors  = rdr.GetOrdinal("GenderCompetitors");
    var ordGenderPercentile   = rdr.GetOrdinal("GenderPercentile");
    var ordCategoryPosition   = rdr.GetOrdinal("CategoryPosition");
    var ordCategoryCompetitors= rdr.GetOrdinal("CategoryCompetitors");
    var ordCategoryPercentile = rdr.GetOrdinal("CategoryPercentile");

    while (await rdr.ReadAsync())
    {
        results.Add(new
        {
            oceanswimsId = rdr.GetInt32(ordOceanSwimsId),
            raceId       = rdr.GetInt32(ordRaceId),

            raceDate = rdr.IsDBNull(ordRaceDate)
                ? (DateTime?)null
                : rdr.GetDateTime(ordRaceDate),

            raceName = rdr.IsDBNull(ordRaceName)
                ? null : rdr.GetString(ordRaceName),

            distance = rdr.IsDBNull(ordDistance)
                ? (decimal?)null : rdr.GetDecimal(ordDistance),

            raceTime = rdr.IsDBNull(ordRaceTime)
                ? (TimeSpan?)null : rdr.GetTimeSpan(ordRaceTime),

            category = rdr.IsDBNull(ordCategory)
                ? null : rdr.GetString(ordCategory),

            gender = rdr.IsDBNull(ordSex)
                ? null : rdr.GetString(ordSex),

            forename = rdr.IsDBNull(ordForename)
                ? null : rdr.GetString(ordForename),

            surname = rdr.IsDBNull(ordSurname)
                ? null : rdr.GetString(ordSurname),

            fullName = rdr.IsDBNull(ordFullName)
                ? null : rdr.GetString(ordFullName),

            overallPosition = rdr.IsDBNull(ordOverallPosition)
                ? (int?)null : rdr.GetInt32(ordOverallPosition),

            overallCompetitors = rdr.IsDBNull(ordOverallCompetitors)
                ? (int?)null : rdr.GetInt32(ordOverallCompetitors),

            overallPercentile = rdr.IsDBNull(ordOverallPercentile)
                ? (decimal?)null : rdr.GetDecimal(ordOverallPercentile),

            genderPosition = rdr.IsDBNull(ordGenderPosition)
                ? (int?)null : rdr.GetInt32(ordGenderPosition),

            genderCompetitors = rdr.IsDBNull(ordGenderCompetitors)
                ? (int?)null : rdr.GetInt32(ordGenderCompetitors),

            genderPercentile = rdr.IsDBNull(ordGenderPercentile)
                ? (decimal?)null : rdr.GetDecimal(ordGenderPercentile),

            categoryPosition = rdr.IsDBNull(ordCategoryPosition)
                ? (int?)null : rdr.GetInt32(ordCategoryPosition),

            categoryCompetitors = rdr.IsDBNull(ordCategoryCompetitors)
                ? (int?)null : rdr.GetInt32(ordCategoryCompetitors),

            categoryPercentile = rdr.IsDBNull(ordCategoryPercentile)
                ? (decimal?)null : rdr.GetDecimal(ordCategoryPercentile)
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

// Secondary search — phonetically similar surnames (for logged-in swimmer checking own name)
// e.g. Hanby → also returns Hamby rows
app.MapGet("/swims/search-similar", async (string forename, string surname) =>
{
    if (string.IsNullOrWhiteSpace(forename) || string.IsNullOrWhiteSpace(surname))
        return Results.BadRequest("forename and surname required");

    var results = new List<object>();

    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();

    var cmd = new SqlCommand("""
        SELECT TOP 200
            oceanswimsid,
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
        WHERE Forename_Search LIKE @forename COLLATE Latin1_General_CI_AI
          AND SOUNDEX(Surname) = SOUNDEX(@surname)
          AND UPPER(Surname) != UPPER(@surname)
        ORDER BY RaceDate DESC
        """, conn);

    cmd.Parameters.AddWithValue("@forename", $"%{forename.Trim()}%");
    cmd.Parameters.AddWithValue("@surname",  surname.Trim());

    using var rdr = await cmd.ExecuteReaderAsync();

    var ordOceanSwimsId        = rdr.GetOrdinal("oceanswimsid");
    var ordRaceId              = rdr.GetOrdinal("raceid");
    var ordRaceDate            = rdr.GetOrdinal("RaceDate");
    var ordRaceName            = rdr.GetOrdinal("RaceName");
    var ordDistance            = rdr.GetOrdinal("Distance");
    var ordRaceTime            = rdr.GetOrdinal("RaceTime");
    var ordCategory            = rdr.GetOrdinal("Category");
    var ordSex                 = rdr.GetOrdinal("Sex");
    var ordForename            = rdr.GetOrdinal("Forename");
    var ordSurname             = rdr.GetOrdinal("Surname");
    var ordFullName            = rdr.GetOrdinal("FullName");
    var ordOverallPosition     = rdr.GetOrdinal("OverallPosition");
    var ordOverallCompetitors  = rdr.GetOrdinal("OverallCompetitors");
    var ordOverallPercentile   = rdr.GetOrdinal("OverallPercentile");
    var ordGenderPosition      = rdr.GetOrdinal("GenderPosition");
    var ordGenderCompetitors   = rdr.GetOrdinal("GenderCompetitors");
    var ordGenderPercentile    = rdr.GetOrdinal("GenderPercentile");
    var ordCategoryPosition    = rdr.GetOrdinal("CategoryPosition");
    var ordCategoryCompetitors = rdr.GetOrdinal("CategoryCompetitors");
    var ordCategoryPercentile  = rdr.GetOrdinal("CategoryPercentile");

    while (await rdr.ReadAsync())
    {
        results.Add(new
        {
            oceanswimsId       = rdr.GetInt32(ordOceanSwimsId),
            raceId             = rdr.GetInt32(ordRaceId),
            raceDate           = rdr.IsDBNull(ordRaceDate)           ? (DateTime?)null : rdr.GetDateTime(ordRaceDate),
            raceName           = rdr.IsDBNull(ordRaceName)           ? null : rdr.GetString(ordRaceName),
            distance           = rdr.IsDBNull(ordDistance)           ? (decimal?)null : rdr.GetDecimal(ordDistance),
            raceTime           = rdr.IsDBNull(ordRaceTime)           ? (TimeSpan?)null : rdr.GetTimeSpan(ordRaceTime),
            category           = rdr.IsDBNull(ordCategory)           ? null : rdr.GetString(ordCategory),
            gender             = rdr.IsDBNull(ordSex)                ? null : rdr.GetString(ordSex),
            forename           = rdr.IsDBNull(ordForename)           ? null : rdr.GetString(ordForename),
            surname            = rdr.IsDBNull(ordSurname)            ? null : rdr.GetString(ordSurname),
            fullName           = rdr.IsDBNull(ordFullName)           ? null : rdr.GetString(ordFullName),
            overallPosition    = rdr.IsDBNull(ordOverallPosition)    ? (int?)null : rdr.GetInt32(ordOverallPosition),
            overallCompetitors = rdr.IsDBNull(ordOverallCompetitors) ? (int?)null : rdr.GetInt32(ordOverallCompetitors),
            overallPercentile  = rdr.IsDBNull(ordOverallPercentile)  ? (decimal?)null : rdr.GetDecimal(ordOverallPercentile),
            genderPosition     = rdr.IsDBNull(ordGenderPosition)     ? (int?)null : rdr.GetInt32(ordGenderPosition),
            genderCompetitors  = rdr.IsDBNull(ordGenderCompetitors)  ? (int?)null : rdr.GetInt32(ordGenderCompetitors),
            genderPercentile   = rdr.IsDBNull(ordGenderPercentile)   ? (decimal?)null : rdr.GetDecimal(ordGenderPercentile),
            categoryPosition   = rdr.IsDBNull(ordCategoryPosition)   ? (int?)null : rdr.GetInt32(ordCategoryPosition),
            categoryCompetitors= rdr.IsDBNull(ordCategoryCompetitors)? (int?)null : rdr.GetInt32(ordCategoryCompetitors),
            categoryPercentile = rdr.IsDBNull(ordCategoryPercentile) ? (decimal?)null : rdr.GetDecimal(ordCategoryPercentile),
            _nearMatch         = true
        });
    }

    return Results.Ok(new { count = results.Count, results });
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
    var ordRaceId      = rdr.GetOrdinal("raceid");
    var ordRaceName    = rdr.GetOrdinal("RaceName");
    var ordRaceDate    = rdr.GetOrdinal("RaceDate");
    var ordDistance    = rdr.GetOrdinal("Distance");
    var ordResultCount = rdr.GetOrdinal("ResultCount");

    while (await rdr.ReadAsync())
    {
        results.Add(new
        {
            raceId = rdr.GetInt32(ordRaceId),

            raceName = rdr.IsDBNull(ordRaceName)
                ? null : rdr.GetString(ordRaceName),

            raceDate = rdr.IsDBNull(ordRaceDate)
                ? null
                : rdr.GetDateTime(ordRaceDate)
                      .ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture),

            distance = rdr.IsDBNull(ordDistance)
                ? (decimal?)null : rdr.GetDecimal(ordDistance),

            resultCount = rdr.GetInt32(ordResultCount)
        });
    }

    return Results.Ok(results);
});

app.MapGet("/results/{slug}", async (string slug, IWebHostEnvironment env) =>
{
    var parts = slug.Split('-');
    if (!int.TryParse(parts.Last(), out var raceId))
        return Results.NotFound();

    using var conn = new SqlConnection(connStr);

    var race = await conn.QueryFirstOrDefaultAsync<string>(
        "SELECT TOP 1 RaceName FROM dbo.vw_OceanSwims_Search WHERE raceid = @raceId",
        new { raceId });

    if (race == null)
        return Results.NotFound();

    var expectedSlug = SlugHelper.GenerateSlug(race);

    if (!slug.StartsWith(expectedSlug))
        return Results.Redirect($"/results/{expectedSlug}-{raceId}", true);

    var filePath = Path.Combine(env.WebRootPath, "index.html");

    if (!File.Exists(filePath))
        return Results.Problem("index.html not found");

    return Results.File(filePath, "text/html");
});

app.Run();

// ── Email helpers ────────────────────────────────────────────────────────────
async Task SendVerificationEmailAsync(string toEmail, string token)
{
    var apiKey    = builder.Configuration["SendGrid:ApiKey"];
    var fromEmail = builder.Configuration["SendGrid:FromEmail"] ?? "noreply@oceanswimmer.com.au";
    var fromName  = builder.Configuration["SendGrid:FromName"]  ?? "OceanSwimmer";
    var baseUrl   = builder.Configuration["App:BaseUrl"]        ?? "https://oceanswimmer.com.au";

    if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_"))
    {
        // SendGrid not configured — log the link so local dev still works
        Console.WriteLine($"[DEV] Verify link: {baseUrl}/auth/verify-email?token={token}");
        return;
    }

    var verifyUrl = $"{baseUrl}/auth/verify-email?token={token}";
    var client    = new SendGridClient(apiKey);
    var msg       = new SendGridMessage
    {
        From            = new EmailAddress(fromEmail, fromName),
        Subject         = "Verify your OceanSwimmer account",
        PlainTextContent = $"Hi,\n\nPlease verify your email address by clicking the link below:\n\n{verifyUrl}\n\nThis link expires in 24 hours.\n\n— The OceanSwimmer team",
        HtmlContent     = $@"
            <p>Hi,</p>
            <p>Please verify your email address to activate your OceanSwimmer account:</p>
            <p style=""margin:24px 0"">
                <a href=""{verifyUrl}""
                   style=""background:#0066cc;color:#fff;padding:12px 24px;border-radius:6px;
                           text-decoration:none;font-weight:600;font-size:15px;"">
                    Verify my email
                </a>
            </p>
            <p style=""color:#888;font-size:13px;"">This link expires in 24 hours. If you didn't register, you can ignore this email.</p>
            <p style=""color:#888;font-size:13px;"">— The OceanSwimmer team</p>"
    };
    msg.AddTo(new EmailAddress(toEmail));
    await client.SendEmailAsync(msg);
}

// ── Request models ───────────────────────────────────────────────────────────
record ClaimAllRequest(List<int> Ids);
record RegisterRequest(string Email, string Password);
record LoginRequest(string Email, string Password);
