using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Data.SqlClient;
using OceanSwimmer.Api.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;
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

        await LogLoginAsync(userId, email, "Google", true, null, ctx.HttpContext);
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

        await LogLoginAsync(userId, email, "Facebook", true, null, ctx.HttpContext);
    };
});

builder.Services.AddAuthorization();

builder.Services.AddHostedService(sp =>
    new LeaderboardRefreshService(
        connStr!,
        sp.GetRequiredService<ILogger<LeaderboardRefreshService>>()));

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

// ── Legacy URL redirects ────────────────────────────────────────────────
// Old race URLs like /?raceId=404 → 301 to /results/<slug>-<id>.
// Fixes the "Duplicate without user-selected canonical" issue in
// Search Console where Google indexed both URL formats for the same race.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" &&
        context.Request.Query.TryGetValue("raceId", out var rid) &&
        int.TryParse(rid, out var raceId))
    {
        try
        {
            using var conn = new SqlConnection(connStr);
            var raceName = await conn.QueryFirstOrDefaultAsync<string?>(
                "SELECT TOP 1 RaceName FROM dbo.vw_OceanSwims_Search WHERE raceid = @raceId",
                new { raceId });

            if (!string.IsNullOrEmpty(raceName))
            {
                var slug = SlugHelper.GenerateSlug(raceName);
                context.Response.Redirect($"/results/{slug}-{raceId}", permanent: true);
                return;
            }
        }
        catch
        {
            // If lookup fails, fall through and serve index.html as normal.
        }
    }
    await next();
});

// UseDefaultFiles removed — "/" is handled explicitly by MapGet below
// so that we can inject server-side content for Googlebot.
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
    // Stamp NotifyOptedInAt only when the box was actually ticked, so we have
    // proof-of-consent. Default off (column default = 0) leaves it null.
    DateTime? optedInAt = req.NotifyUnclaimedResults ? DateTime.UtcNow : (DateTime?)null;

    await conn.ExecuteAsync(@"
        INSERT INTO auth.Users (
            Email, AuthProvider, ProviderId, CreatedAt, PasswordHash,
            EmailVerified, VerificationToken, VerificationTokenExpiry,
            NotifyUnclaimedResults, NotifyOptedInAt)
        VALUES (
            @email, 'Local', @email, GETUTCDATE(), @hash,
            0, @token, @expiry,
            @notify, @optedInAt)",
        new { email, hash, token, expiry,
              notify = req.NotifyUnclaimedResults,
              optedInAt });

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

    await LogLoginAsync(userId, (string)user.Email, "EmailVerification", true, null, ctx);

    return Results.Redirect("/?verified=1");
});

// Sign in with email + password
app.MapPost("/auth/login-password", async (LoginRequest req, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
    {
        await LogLoginAsync(null, req.Email, "Local", false, "missing-credentials", ctx);
        return Results.BadRequest(new { error = "Email and password are required." });
    }

    var email = req.Email.Trim().ToLower();

    using var conn = new SqlConnection(connStr);

    var user = await conn.QueryFirstOrDefaultAsync(@"
        SELECT UserId, Email, PasswordHash, EmailVerified
        FROM auth.Users
        WHERE Email = @email AND AuthProvider = 'Local'",
        new { email });

    // Deliberate vague error — don't reveal whether email exists
    if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, (string)(user.PasswordHash ?? "")))
    {
        await LogLoginAsync(
            user == null ? (int?)null : (int)user.UserId,
            email,
            "Local",
            false,
            user == null ? "no-such-user" : "invalid-password",
            ctx);
        return Results.BadRequest(new { error = "Invalid email or password." });
    }

    if (!(bool)user.EmailVerified)
    {
        await LogLoginAsync((int)user.UserId, email, "Local", false, "unverified-email", ctx);
        return Results.BadRequest(new { error = "Please verify your email address before signing in." });
    }

    int userId = (int)user.UserId;

    var claims = new List<Claim>
    {
        new Claim("userId", userId.ToString()),
        new Claim(ClaimTypes.Email, (string)user.Email)
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    await ctx.SignInAsync("Cookies", principal);

    await LogLoginAsync(userId, email, "Local", true, null, ctx);

    return Results.Ok(new { message = "Signed in." });
});


// Request password reset — always returns 200 to avoid email enumeration
app.MapPost("/auth/forgot-password", async (ForgotPasswordRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { error = "Email is required." });

    var email = req.Email.Trim().ToLower();

    using var conn = new SqlConnection(connStr);

    var userId = await conn.QueryFirstOrDefaultAsync<int?>(
        "SELECT UserId FROM auth.Users WHERE Email = @email AND AuthProvider = 'Local'",
        new { email });

    if (userId != null)
    {
        var token  = Guid.NewGuid().ToString("N");
        var expiry = DateTime.UtcNow.AddHours(1);

        await conn.ExecuteAsync(
            "UPDATE auth.Users SET ResetToken = @token, ResetTokenExpiry = @expiry WHERE UserId = @userId",
            new { token, expiry, userId });

        await SendPasswordResetEmailAsync(email, token);
    }

    return Results.Ok(new { message = "If an account with that email exists, you will receive a reset link shortly." });
});

// Reset password using the token from the email link
app.MapPost("/auth/reset-password", async (ResetPasswordRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
        return Results.BadRequest(new { error = "Token and new password are required." });

    if (req.NewPassword.Length < 8)
        return Results.BadRequest(new { error = "Password must be at least 8 characters." });

    using var conn = new SqlConnection(connStr);

    var user = await conn.QueryFirstOrDefaultAsync(@"
        SELECT UserId FROM auth.Users
        WHERE ResetToken = @token
          AND ResetTokenExpiry > GETUTCDATE()
          AND AuthProvider = 'Local'",
        new { token = req.Token });

    if (user == null)
        return Results.BadRequest(new { error = "This reset link has expired or is invalid." });

    var hash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

    await conn.ExecuteAsync(@"
        UPDATE auth.Users
        SET PasswordHash = @hash, ResetToken = NULL, ResetTokenExpiry = NULL
        WHERE UserId = @userId",
        new { hash, userId = (int)user.UserId });

    return Results.Ok(new { message = "Password updated. You can now sign in with your new password." });
});


// ---------------------------------------------------------------------------
// ACCOUNT SETTINGS  (requires login)
// ---------------------------------------------------------------------------

// Get current account settings — notification prefs + email (shown in the
// delete-account confirmation) + auth provider (so the UI can decide whether
// to show e.g. "Change password" for Local-only accounts).
app.MapGet("/auth/settings", async (HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);
    var settings = await conn.QueryFirstOrDefaultAsync(@"
        SELECT Email, NotifyUnclaimedResults, NotifyOptedInAt, AuthProvider
        FROM auth.Users WHERE UserId = @userId",
        new { userId });

    if (settings == null) return Results.Unauthorized();

    return Results.Ok(new
    {
        email                  = (string)settings.Email,
        notifyUnclaimedResults = (bool)settings.NotifyUnclaimedResults,
        notifyOptedInAt        = (DateTime?)settings.NotifyOptedInAt,
        authProvider           = (string)settings.AuthProvider
    });
});

// Update notification preference. NotifyOptedInAt is stamped only on the
// first 0 -> 1 transition (proof-of-consent date); once set, we preserve it
// permanently — even if the member opts out and back in — so we always have
// evidence of when they originally agreed.
app.MapPut("/auth/settings", async (UpdateSettingsRequest req, HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);
    await conn.ExecuteAsync(@"
        UPDATE auth.Users
        SET NotifyUnclaimedResults = @notify,
            NotifyOptedInAt = CASE
                WHEN @notify = 1 AND NotifyOptedInAt IS NULL THEN GETUTCDATE()
                ELSE NotifyOptedInAt
            END
        WHERE UserId = @userId",
        new { userId, notify = req.NotifyUnclaimedResults });

    return Results.Ok(new { ok = true });
});

// Delete the account. The user must type their email address as
// confirmation. Cascade order is auth.AthleteResults (the FK side) then
// auth.Users; both inside a single transaction so a partial delete can't
// orphan rows. Signs the user out at the end.
//
// POST rather than DELETE because we need a request body for the email
// confirmation, and DELETE-with-body is unreliable through some HTTP
// middleware / proxies.
app.MapPost("/auth/delete-account", async (DeleteAccountRequest req, HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirst("userId")?.Value;
    if (userIdStr == null) return Results.Unauthorized();
    var userId = int.Parse(userIdStr);

    using var conn = new SqlConnection(connStr);
    var actualEmail = await conn.QueryFirstOrDefaultAsync<string>(
        "SELECT Email FROM auth.Users WHERE UserId = @userId",
        new { userId });

    if (actualEmail == null) return Results.Unauthorized();

    var typed = (req.EmailConfirmation ?? "").Trim().ToLower();
    if (typed != actualEmail.ToLower())
        return Results.BadRequest(new { error = "Confirmation email did not match." });

    await conn.OpenAsync();
    using var tx = conn.BeginTransaction();
    try
    {
        await conn.ExecuteAsync(
            "DELETE FROM auth.AthleteResults WHERE UserId = @userId",
            new { userId }, tx);
        await conn.ExecuteAsync(
            "DELETE FROM auth.Users WHERE UserId = @userId",
            new { userId }, tx);
        tx.Commit();
    }
    catch
    {
        tx.Rollback();
        throw;
    }

    await ctx.SignOutAsync("Cookies");
    return Results.Ok(new { ok = true });
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
            ar.IsARace,
            rt.RaceTypeDescription
        FROM auth.AthleteResults ar
        JOIN dbo.vw_OceanSwims_Search o ON o.oceanswimsid = ar.OceanSwimsId
        LEFT JOIN dbo.Race r ON r.raceid = o.raceid
        LEFT JOIN RaceResult.dbo.RaceType rt ON rt.racetypeid = r.racetypeid
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

    var name     = form["FeedbackName"].ToString().Trim();
    var email    = form["FeedbackEmail"].ToString().Trim();
    var type     = form["RequestType"].ToString().Trim();
    var message  = form["FeedbackMessage"].ToString().Trim();
    var pageUrl  = form["PageUrl"].ToString().Trim();
    var honeypot = form["Website"].ToString();   // hidden field — humans never fill this

    // Browser metadata for triage / additional spam signals
    var userIp    = request.HttpContext.Connection.RemoteIpAddress?.ToString();
    var userAgent = request.Headers.UserAgent.ToString();

    // ── Spam guards ──────────────────────────────────────────────────────────
    // Each guard returns the thanks page silently so bots don't learn what
    // tripped them. Real users will never hit these branches.

    // 1. Honeypot — bots fill every field, including hidden ones
    if (!string.IsNullOrWhiteSpace(honeypot))
    {
        Console.WriteLine($"[Feedback] Rejected (honeypot): ip={userIp} ua={userAgent}");
        return Results.Redirect("/feedback-thanks.html");
    }

    // 2. PageUrl is set client-side via window.location.href. Bots that POST
    //    directly to /api/feedback skip the script entirely and leave it blank
    //    — or stuff a junk value that doesn't match our domain.
    if (string.IsNullOrWhiteSpace(pageUrl) ||
        !(pageUrl.Contains("oceanswimmer.com.au", StringComparison.OrdinalIgnoreCase) ||
          pageUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine($"[Feedback] Rejected (bad PageUrl='{pageUrl}'): ip={userIp}");
        return Results.Redirect("/feedback-thanks.html");
    }

    // 3. Real browsers always send a User-Agent. Empty/missing = scripted POST.
    if (string.IsNullOrWhiteSpace(userAgent))
    {
        Console.WriteLine($"[Feedback] Rejected (no User-Agent): ip={userIp}");
        return Results.Redirect("/feedback-thanks.html");
    }

    // 4. Required field — surface a real error here, this can hit real users
    if (string.IsNullOrWhiteSpace(message))
        return Results.BadRequest(new { error = "Message is required." });

    // 5. Link-stuffed marketing spam — count URLs in the message
    var urlCount = System.Text.RegularExpressions.Regex.Matches(
        message, @"https?://", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
    if (urlCount > 2)
    {
        Console.WriteLine($"[Feedback] Rejected (too many URLs={urlCount}): ip={userIp}");
        return Results.Redirect("/feedback-thanks.html");
    }

    // 6. Short pitch + a link is a classic bot pattern (e.g. "Hi check this out https://...")
    if (message.Length < 40 && urlCount > 0)
    {
        Console.WriteLine($"[Feedback] Rejected (short+link): ip={userIp}");
        return Results.Redirect("/feedback-thanks.html");
    }

    using var conn = new SqlConnection(connStr);

    await conn.ExecuteAsync(@"
        INSERT INTO dbo.Feedback
        (FeedbackName, FeedbackEmail, RequestType, FeedbackMessage, PageUrl, UserIP, UserAgent)
        VALUES
        (@name, @email, @type, @message, @pageUrl, @userIp, @userAgent)",
        new { name, email, type, message, pageUrl, userIp, userAgent });

    return Results.Redirect("/feedback-thanks.html");
});

app.MapGet("/api/race-count", async () =>
{
    using var conn = new SqlConnection(connStr);

    var count = await conn.QuerySingleAsync<int>(
        "SELECT COUNT(DISTINCT raceid) FROM dbo.vw_OceanSwims_Search"
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
    HttpContext httpCtx,
    string? forename,
    string? surname,
    string? race,
    int?    raceId,
    string? category,
    string? gender,
    int page     = 1,
    int pageSize = 750) =>
{
    page = Math.Max(page, 1);

    // Detect crawlers — they don't need full result sets, and unbounded
    // pageSize chews bandwidth (Googlebot was pulling 2K+ rows per race).
    string ua = httpCtx.Request.Headers.UserAgent.ToString();
    bool isCrawler = !string.IsNullOrEmpty(ua) && (
        ua.Contains("bot",     StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("crawler", StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("spider",  StringComparison.OrdinalIgnoreCase));

    if (isCrawler)
        pageSize = Math.Clamp(pageSize, 1, 250);        // bots — keep tight
    else if (raceId != null)
        pageSize = Math.Clamp(pageSize, 1, 10_000);     // race page — full results
    else
        pageSize = Math.Clamp(pageSize, 1, 750);        // name search — headroom for common surnames

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

    await LogSearchAsync(
        "swims", httpCtx,
        forename: forename, surname: surname, race: race, raceId: raceId,
        category: category, gender: gender, page: page, pageSize: pageSize,
        resultCount: results.Count);

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
app.MapGet("/swims/search-similar", async (HttpContext httpCtx, string forename, string surname) =>
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

    await LogSearchAsync(
        "swims-similar", httpCtx,
        forename: forename, surname: surname,
        resultCount: results.Count);

    return Results.Ok(new { count = results.Count, results });
});

// ---------------------------------------------------------------------------
// LEADERBOARD ENDPOINTS
// ---------------------------------------------------------------------------

app.MapGet("/leaderboard/alltime", async () =>
{
    using var conn = new SqlConnection(connStr);
    var rows = await conn.QueryAsync(@"
        SELECT TOP 1000
            Forename,
            Surname,
            FullName,
            Gender,
            TotalSwims,
            TotalDistanceKm,
            FirstSwimDate,
            LastSwimDate
        FROM dbo.LeaderboardAllTime
        ORDER BY TotalSwims DESC, TotalDistanceKm DESC");

    return Results.Ok(rows.Select(r => new
    {
        forename        = (string?)r.Forename,
        surname         = (string?)r.Surname,
        fullName        = (string?)r.FullName,
        gender          = (string?)r.Gender,
        totalSwims      = (int)r.TotalSwims,
        totalDistanceKm = (decimal?)r.TotalDistanceKm,
        firstSwimDate   = (DateTime?)r.FirstSwimDate,
        lastSwimDate    = (DateTime?)r.LastSwimDate
    }));
});

app.MapGet("/leaderboard/seasons", async () =>
{
    using var conn = new SqlConnection(connStr);
    var seasons = await conn.QueryAsync<int>(
        "SELECT DISTINCT Season FROM dbo.LeaderboardSeasonal ORDER BY Season DESC");
    return Results.Ok(seasons);
});

app.MapGet("/leaderboard/seasonal", async (int? season) =>
{
    var targetSeason = season ?? DateTime.Now.Year;
    using var conn = new SqlConnection(connStr);
    var rows = await conn.QueryAsync(@"
        SELECT TOP 1000
            Forename,
            Surname,
            FullName,
            Gender,
            TotalSwims,
            TotalDistanceKm
        FROM dbo.LeaderboardSeasonal
        WHERE Season = @targetSeason
        ORDER BY TotalSwims DESC, TotalDistanceKm DESC",
        new { targetSeason });

    return Results.Ok(rows.Select(r => new
    {
        forename        = (string?)r.Forename,
        surname         = (string?)r.Surname,
        fullName        = (string?)r.FullName,
        gender          = (string?)r.Gender,
        totalSwims      = (int)r.TotalSwims,
        totalDistanceKm = (decimal?)r.TotalDistanceKm
    }));
});

// Race leaderboard — all editions straight from vw_OceanSwims_Rank.
// The view includes RaceId (MIN(raceid)) so the frontend can link to results.
app.MapGet("/leaderboard/races", async () =>
{
    using var conn = new SqlConnection(connStr);
    var rows = await conn.QueryAsync(@"
        SELECT
            RaceId,
            RaceName,
            RaceDate,
            Races               AS Events,
            OverallCompetitors  AS TotalCompetitors
        FROM dbo.vw_OceanSwims_Rank
        ORDER BY OverallCompetitors DESC");

    return Results.Ok(rows.Select(r => new
    {
        raceId           = (int)r.RaceId,
        raceName         = (string?)r.RaceName,
        raceDate         = (DateTime?)r.RaceDate,
        events           = (int)r.Events,
        totalCompetitors = (int)r.TotalCompetitors
    }));
});

// Distinct years available in the race leaderboard (for the season picker).
app.MapGet("/leaderboard/race-seasons", async () =>
{
    using var conn = new SqlConnection(connStr);
    var years = await conn.QueryAsync<int>(
        "SELECT DISTINCT YEAR(RaceDate) FROM dbo.vw_OceanSwims_Rank ORDER BY 1 DESC");
    return Results.Ok(years);
});

// Top races for a single season — filtered from vw_OceanSwims_Rank by year.
app.MapGet("/leaderboard/races/seasonal", async (int? year) =>
{
    var targetYear = year ?? DateTime.Now.Year;
    using var conn = new SqlConnection(connStr);
    var rows = await conn.QueryAsync(@"
        SELECT
            RaceId,
            RaceName,
            RaceDate,
            Races               AS Events,
            OverallCompetitors  AS TotalCompetitors
        FROM dbo.vw_OceanSwims_Rank
        WHERE YEAR(RaceDate) = @targetYear
        ORDER BY OverallCompetitors DESC",
        new { targetYear });

    return Results.Ok(rows.Select(r => new
    {
        raceId           = (int)r.RaceId,
        raceName         = (string?)r.RaceName,
        raceDate         = (DateTime?)r.RaceDate,
        events           = (int)r.Events,
        totalCompetitors = (int)r.TotalCompetitors
    }));
});

// ── Podium leaderboards ──────────────────────────────────────────────────────

// Distinct seasons available for the podium leaderboard.
app.MapGet("/leaderboard/podium/seasons", async () =>
{
    using var conn = new SqlConnection(connStr);
    var seasons = await conn.QueryAsync<int>(
        "SELECT DISTINCT YEAR(RaceDate) FROM dbo.vw_OceanSwims_Search WHERE OverallPosition IS NOT NULL ORDER BY 1 DESC");
    return Results.Ok(seasons);
});

// Podium leaderboard — all time.
// Reads from the pre-computed dbo.PodiumLeaderboardAllTime table
// (populated weekly by sp_PopulatePodiumLeaderboards).
app.MapGet("/leaderboard/podium/alltime", async () =>
{
    using var conn = new SqlConnection(connStr);
    var rows = await conn.QueryAsync(@"
        SELECT Forename, Surname, FullName, Gender, TotalSwims, Firsts, Seconds, Thirds, TopTens, AvgPercentile
        FROM dbo.PodiumLeaderboardAllTime
        WHERE HasOverallPodium = 1
        ORDER BY Firsts DESC, Seconds DESC, Thirds DESC, TopTens DESC");

    return Results.Ok(rows.Select(r => new
    {
        forename      = (string?)r.Forename,
        surname       = (string?)r.Surname,
        fullName      = (string?)r.FullName,
        gender        = (string?)r.Gender,
        totalSwims    = (int)r.TotalSwims,
        firsts        = (int)r.Firsts,
        seconds       = (int)r.Seconds,
        thirds        = (int)r.Thirds,
        topTens       = (int)r.TopTens,
        avgPercentile = r.AvgPercentile == null ? (double?)null : (double)r.AvgPercentile
    }));
});

// Podium leaderboard — single season.
// Reads from the pre-computed dbo.PodiumLeaderboardSeasonal table.
app.MapGet("/leaderboard/podium/seasonal", async (int? season) =>
{
    var targetSeason = season ?? DateTime.Now.Year;
    using var conn = new SqlConnection(connStr);
    var rows = await conn.QueryAsync(@"
        SELECT Forename, Surname, FullName, Gender, TotalSwims, Firsts, Seconds, Thirds, TopTens, AvgPercentile
        FROM dbo.PodiumLeaderboardSeasonal
        WHERE Season = @targetSeason
          AND HasOverallPodium = 1
        ORDER BY Firsts DESC, Seconds DESC, Thirds DESC, TopTens DESC",
        new { targetSeason });

    return Results.Ok(rows.Select(r => new
    {
        forename      = (string?)r.Forename,
        surname       = (string?)r.Surname,
        fullName      = (string?)r.FullName,
        gender        = (string?)r.Gender,
        totalSwims    = (int)r.TotalSwims,
        firsts        = (int)r.Firsts,
        seconds       = (int)r.Seconds,
        thirds        = (int)r.Thirds,
        topTens       = (int)r.TopTens,
        avgPercentile = r.AvgPercentile == null ? (double?)null : (double)r.AvgPercentile
    }));
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

// ── Homepage — server-side content injection for Googlebot ──────────────────
// UseDefaultFiles is removed, so this handler owns "/". It pre-populates the
// race count and injects a plain HTML results table for the latest race so
// Googlebot sees real content before the JS DataTable hydrates.
app.MapGet("/", async (IWebHostEnvironment env) =>
{
    var filePath = Path.Combine(env.WebRootPath, "index.html");
    if (!File.Exists(filePath))
        return Results.Problem("index.html not found");

    using var conn = new SqlConnection(connStr);

    var raceCount = await conn.ExecuteScalarAsync<int>(
        "SELECT COUNT(DISTINCT raceid) FROM dbo.vw_OceanSwims_Search");

    var rows = (await conn.QueryAsync(
        """
        SELECT RaceName, RaceDate, FullName, RaceTime,
               OverallPosition, OverallCompetitors,
               Category, CategoryPosition
        FROM   dbo.vw_LatestRaceResults
        ORDER  BY OverallPosition
        """)).AsList();

    var html = await File.ReadAllTextAsync(filePath);

    // Pre-populate race count (JS will overwrite, but Googlebot sees it)
    html = html.Replace(
        "<div id=\"raceCount\" style=\"color:#666; margin-bottom:15px;\"></div>",
        $"<div id=\"raceCount\" style=\"color:#666; margin-bottom:15px;\">{raceCount:N0} races tracked</div>");

    // Build an SSR results block Googlebot can read; JS removes it on load
    if (rows.Count > 0)
    {
        var first   = rows[0];
        string raceName = first.RaceName ?? "";
        string dateStr  = first.RaceDate != null
            ? ((DateTime)first.RaceDate).ToString("d MMM yyyy") : "";

        var sb = new StringBuilder();
        sb.Append("<div id=\"ssr-latest-race\">");
        sb.Append($"<p style=\"font-weight:600\">" +
                  $"{System.Net.WebUtility.HtmlEncode(raceName)}" +
                  $" &mdash; {dateStr}" +
                  $" &mdash; {rows.Count} results</p>");
        sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:14px\">");
        sb.Append("<thead><tr>");
        foreach (var h in new[] { "Pos", "Name", "Time", "Category", "Cat Pos" })
            sb.Append($"<th style=\"text-align:left;padding:4px 8px;border-bottom:1px solid #ddd\">{h}</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var r in rows)
        {
            sb.Append("<tr>");
            sb.Append($"<td style=\"padding:3px 8px\">{r.OverallPosition}</td>");
            sb.Append($"<td style=\"padding:3px 8px\">{System.Net.WebUtility.HtmlEncode((string)(r.FullName   ?? ""))}</td>");
            sb.Append($"<td style=\"padding:3px 8px\">{r.RaceTime}</td>");
            sb.Append($"<td style=\"padding:3px 8px\">{System.Net.WebUtility.HtmlEncode((string)(r.Category  ?? ""))}</td>");
            sb.Append($"<td style=\"padding:3px 8px\">{r.CategoryPosition}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table></div>");

        html = html.Replace(
            "<table id=\"resultsTable\"",
            sb.ToString() + "\n    <table id=\"resultsTable\"");
    }

    return Results.Content(html, "text/html");
});

app.MapGet("/results/{slug}", async (string slug, IWebHostEnvironment env) =>
{
    // Googlebot's URL extractor sometimes scrapes literal JS template-literal
    // strings (e.g. "/results/${generateSlug(r.RaceName)}-${r.raceid}") out of
    // <script> bodies before JS runs, and then tries to crawl them. Return 410
    // Gone so those URLs get de-indexed quickly instead of lingering as 404s.
    if (slug.IndexOfAny(new[] { '$', '{', '}' }) >= 0)
        return Results.StatusCode(410); // Gone

    var parts = slug.Split('-');
    if (!int.TryParse(parts.Last(), out var raceId))
        return Results.NotFound();

    using var conn = new SqlConnection(connStr);

    var race = await conn.QueryFirstOrDefaultAsync(
        """
        SELECT TOP 1 v.RaceName, v.RaceDate, v.Distance, v.OverallCompetitors, r.Location
        FROM dbo.vw_OceanSwims_Search v
        LEFT JOIN dbo.Race r ON r.raceid = v.raceid
        WHERE v.raceid = @raceId
        """,
        new { raceId });

    if (race == null)
        return Results.NotFound();

    string raceName = race.RaceName ?? "";
    var expectedSlug = SlugHelper.GenerateSlug(raceName);

    if (!slug.StartsWith(expectedSlug))
        return Results.Redirect($"/results/{expectedSlug}-{raceId}", true);

    var filePath = Path.Combine(env.WebRootPath, "index.html");

    if (!File.Exists(filePath))
        return Results.Problem("index.html not found");

    var html = await File.ReadAllTextAsync(filePath);

    // Build race-specific title and description
    var dateStr = race.RaceDate != null
        ? ((DateTime)race.RaceDate).ToString("d MMM yyyy")
        : "";
    var distStr = race.Distance != null ? $"{race.Distance}km " : "";
    var countStr = race.OverallCompetitors != null ? $"{race.OverallCompetitors} finishers. " : "";
    var description = $"Results for {raceName}{(dateStr != "" ? " – " + dateStr : "")}. {countStr}View times, positions and category results on OceanSwimmer.";

    // Canonical URL for this race — strips any ?category=, ?gender=, etc.
    // so all filter variants consolidate to the same SEO target.
    var canonicalUrl = $"https://oceanswimmer.com.au/results/{expectedSlug}-{raceId}";

    // Build JSON-LD structured data (SportsEvent) so Googlebot sees real content
    // even before the React app hydrates, preventing soft-404 classification.
    var ldObj = new Dictionary<string, object?>
    {
        ["@context"]            = "https://schema.org",
        ["@type"]               = "SportsEvent",
        ["name"]                = raceName,
        ["url"]                 = canonicalUrl,
        ["description"]         = description,
        ["sport"]               = "Ocean swimming",
        ["eventStatus"]         = "https://schema.org/EventScheduled",
        ["eventAttendanceMode"] = "https://schema.org/OfflineEventAttendanceMode",
    };
    if (race.RaceDate != null)
        ldObj["startDate"] = ((DateTime)race.RaceDate).ToString("yyyy-MM-dd");
    if (race.Distance != null)
        ldObj["distance"] = new Dictionary<string, object>
        {
            ["@type"]    = "QuantitativeValue",
            ["value"]    = (int)Math.Round((double)race.Distance * 1000),
            ["unitCode"] = "MTR",
        };
    if (!string.IsNullOrWhiteSpace((string?)race.Location))
        ldObj["location"] = new Dictionary<string, object>
        {
            ["@type"] = "Place",
            ["name"]  = (string)race.Location!,
        };

    var jsonLd = System.Text.Json.JsonSerializer.Serialize(
        ldObj,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

    var jsonLdScript =
        "\n    <script type=\"application/ld+json\">\n    " +
        jsonLd.Replace("\n", "\n    ") +
        "\n    </script>";

    // Fetch top results for this race to render server-side.
    // Googlebot sees a real table; the JS DataTable replaces it on load.
    var raceRows = (await conn.QueryAsync(
        """
        SELECT TOP 500
               OverallPosition, FullName, RaceTime,
               Category, CategoryPosition, Gender
        FROM   dbo.vw_OceanSwims_Search
        WHERE  raceid = @raceId
        ORDER  BY OverallPosition
        """,
        new { raceId })).AsList();

    // Strip the static homepage canonical so we don't end up with two tags.
    // (Use [^>]* not [^/]* so we don't choke on slashes in the href URL.)
    html = System.Text.RegularExpressions.Regex.Replace(
        html,
        @"<link\s+rel=""canonical""[^>]*>\s*",
        "");

    html = html.Replace(
        "<title>OceanSwimmer Results Search</title>",
        $"<title>{System.Net.WebUtility.HtmlEncode(raceName)} Results | OceanSwimmer</title>\n" +
        $"    <meta name=\"description\" content=\"{System.Net.WebUtility.HtmlEncode(description)}\" />\n" +
        $"    <link rel=\"canonical\" href=\"{canonicalUrl}\" />" +
        jsonLdScript);

    // Inject SSR results table so Googlebot sees real content before JS runs.
    if (raceRows.Count > 0)
    {
        var ssrSb = new StringBuilder();
        ssrSb.Append("<div id=\"ssr-race-results\">");
        ssrSb.Append($"<h1 style=\"font-size:1.3em;margin-bottom:8px\">" +
                     $"{System.Net.WebUtility.HtmlEncode(raceName)}" +
                     (dateStr != "" ? $" &mdash; {dateStr}" : "") +
                     $"</h1>");
        ssrSb.Append($"<p style=\"margin-bottom:8px;color:#555\">{raceRows.Count} finishers</p>");
        ssrSb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:14px\">");
        ssrSb.Append("<thead><tr>");
        foreach (var h in new[] { "Pos", "Name", "Time", "Category", "Cat Pos" })
            ssrSb.Append($"<th style=\"text-align:left;padding:4px 8px;border-bottom:1px solid #ddd\">{h}</th>");
        ssrSb.Append("</tr></thead><tbody>");
        foreach (var r in raceRows)
        {
            ssrSb.Append("<tr>");
            ssrSb.Append($"<td style=\"padding:3px 8px\">{r.OverallPosition}</td>");
            ssrSb.Append($"<td style=\"padding:3px 8px\">{System.Net.WebUtility.HtmlEncode((string)(r.FullName ?? ""))}</td>");
            ssrSb.Append($"<td style=\"padding:3px 8px\">{r.RaceTime}</td>");
            ssrSb.Append($"<td style=\"padding:3px 8px\">{System.Net.WebUtility.HtmlEncode((string)(r.Category ?? ""))}</td>");
            ssrSb.Append($"<td style=\"padding:3px 8px\">{r.CategoryPosition}</td>");
            ssrSb.Append("</tr>");
        }
        ssrSb.Append("</tbody></table></div>");

        html = html.Replace(
            "<table id=\"resultsTable\"",
            ssrSb.ToString() + "\n    <table id=\"resultsTable\"");
    }

    return Results.Content(html, "text/html");
});

app.Run();

// ── Login log helper ─────────────────────────────────────────────────────────
// Writes one row to auth.LoginLog per login attempt. Wrapped in try/catch so
// a logging failure (e.g. transient DB blip) never breaks the actual sign-in.
async Task LogLoginAsync(
    int? userId,
    string? email,
    string authProvider,
    bool success,
    string? failureReason,
    HttpContext? httpCtx)
{
    try
    {
        var ip = httpCtx?.Connection.RemoteIpAddress?.ToString();
        var ua = httpCtx?.Request.Headers["User-Agent"].ToString();
        if (!string.IsNullOrEmpty(ua) && ua.Length > 500)
            ua = ua.Substring(0, 500);

        using var conn = new SqlConnection(connStr);
        await conn.ExecuteAsync(@"
            INSERT INTO auth.LoginLog
                (UserId, Email, AuthProvider, Success, FailureReason, IpAddress, UserAgent)
            VALUES
                (@userId, @email, @authProvider, @success, @failureReason, @ip, @ua)",
            new { userId, email, authProvider, success, failureReason, ip, ua });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LoginLog] Failed to record login: {ex.Message}");
    }
}

// ── Search log helper ────────────────────────────────────────────────────────
// Writes one row to dbo.SearchLog per search call. UserId comes from the
// "userId" claim if signed in, otherwise null. Wrapped in try/catch — a
// logging blip should never break a search.
async Task LogSearchAsync(
    string searchType,
    HttpContext httpCtx,
    string? forename = null,
    string? surname = null,
    string? race = null,
    int? raceId = null,
    string? category = null,
    string? gender = null,
    int? page = null,
    int? pageSize = null,
    int? resultCount = null)
{
    try
    {
        int? userId = null;
        var userIdStr = httpCtx?.User?.FindFirst("userId")?.Value;
        if (int.TryParse(userIdStr, out var parsedUserId))
            userId = parsedUserId;

        var ip = httpCtx?.Connection.RemoteIpAddress?.ToString();
        var ua = httpCtx?.Request.Headers["User-Agent"].ToString();
        if (!string.IsNullOrEmpty(ua) && ua.Length > 500)
            ua = ua.Substring(0, 500);

        var url = httpCtx?.Request.GetDisplayUrl();
        if (!string.IsNullOrEmpty(url) && url.Length > 1000)
            url = url.Substring(0, 1000);

        using var conn = new SqlConnection(connStr);
        await conn.ExecuteAsync(@"
            INSERT INTO dbo.SearchLog
                (UserId, SearchType, Forename, Surname, Race, RaceId,
                 Category, Gender, Page, PageSize, ResultCount, IpAddress, UserAgent, Url)
            VALUES
                (@userId, @searchType, @forename, @surname, @race, @raceId,
                 @category, @gender, @page, @pageSize, @resultCount, @ip, @ua, @url)",
            new {
                userId, searchType, forename, surname, race, raceId,
                category, gender, page, pageSize, resultCount, ip, ua, url
            });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SearchLog] Failed to record search: {ex.Message}");
    }
}

// ── Email helpers ────────────────────────────────────────────────────────────
async Task SendVerificationEmailAsync(string toEmail, string token)
{
    var apiKey    = builder.Configuration["Resend:ApiKey"];
    var fromEmail = builder.Configuration["Resend:FromEmail"] ?? "noreply@oceanswimmer.com.au";
    var fromName  = builder.Configuration["Resend:FromName"]  ?? "OceanSwimmer";
    var baseUrl   = builder.Configuration["App:BaseUrl"]       ?? "https://oceanswimmer.com.au";

    if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_"))
    {
        // Resend not configured — log the link so local dev still works
        Console.WriteLine($"[DEV] Verify link: {baseUrl}/auth/verify-email?token={token}");
        return;
    }

    var verifyUrl = $"{baseUrl}/auth/verify-email?token={token}";
    await SendResendEmailAsync(apiKey, fromEmail, fromName, toEmail,
        subject: "Verify your OceanSwimmer account",
        text: $"Hi,\n\nPlease verify your email address by clicking the link below:\n\n{verifyUrl}\n\nThis link expires in 24 hours.\n\n— The OceanSwimmer team",
        html: $@"
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
            <p style=""color:#888;font-size:13px;"">— The OceanSwimmer team</p>");
}

async Task SendPasswordResetEmailAsync(string toEmail, string token)
{
    var apiKey    = builder.Configuration["Resend:ApiKey"];
    var fromEmail = builder.Configuration["Resend:FromEmail"] ?? "noreply@oceanswimmer.com.au";
    var fromName  = builder.Configuration["Resend:FromName"]  ?? "OceanSwimmer";
    var baseUrl   = builder.Configuration["App:BaseUrl"]       ?? "https://oceanswimmer.com.au";

    var resetUrl = $"{baseUrl}/reset-password.html?token={token}";

    if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_"))
    {
        Console.WriteLine($"[DEV] Password reset link: {resetUrl}");
        return;
    }

    await SendResendEmailAsync(apiKey, fromEmail, fromName, toEmail,
        subject: "Reset your OceanSwimmer password",
        text: $"Hi,\n\nYou requested a password reset for your OceanSwimmer account. Click the link below to set a new password:\n\n{resetUrl}\n\nThis link expires in 1 hour. If you didn't request this, you can safely ignore this email.\n\n— The OceanSwimmer team",
        html: $@"
            <p>Hi,</p>
            <p>You requested a password reset for your OceanSwimmer account.</p>
            <p style=""margin:24px 0"">
                <a href=""{resetUrl}""
                   style=""background:#0066cc;color:#fff;padding:12px 24px;border-radius:6px;
                           text-decoration:none;font-weight:600;font-size:15px;"">
                    Reset my password
                </a>
            </p>
            <p style=""color:#888;font-size:13px;"">This link expires in 1 hour. If you didn't request a password reset, you can safely ignore this email.</p>
            <p style=""color:#888;font-size:13px;"">— The OceanSwimmer team</p>");
}

async Task SendResendEmailAsync(string apiKey, string fromEmail, string fromName,
    string toEmail, string subject, string text, string html)
{
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

    var payload = JsonSerializer.Serialize(new
    {
        from    = $"{fromName} <{fromEmail}>",
        to      = new[] { toEmail },
        subject,
        text,
        html
    });

    var response = await http.PostAsync(
        "https://api.resend.com/emails",
        new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[Resend] Error {(int)response.StatusCode}: {body}");
    }
}

// ── Request models ───────────────────────────────────────────────────────────
record ClaimAllRequest(List<int> Ids);
record RegisterRequest(string Email, string Password, bool NotifyUnclaimedResults = false);
record LoginRequest(string Email, string Password);
record ForgotPasswordRequest(string Email);
record ResetPasswordRequest(string Token, string NewPassword);
record UpdateSettingsRequest(bool NotifyUnclaimedResults);
record DeleteAccountRequest(string EmailConfirmation);


// ── Leaderboard refresh ──────────────────────────────────────────────────────
// Runs sp_PopulateLeaderboards every Sunday at 14:00 UTC (≈ midnight AEST).
public class LeaderboardRefreshService : BackgroundService
{
    private readonly string _connStr;
    private readonly ILogger<LeaderboardRefreshService> _logger;

    // Target time: Sunday 14:00 UTC ≈ midnight Australian Eastern time
    private static readonly TimeOnly TargetTime = new(14, 0, 0);

    public LeaderboardRefreshService(string connStr, ILogger<LeaderboardRefreshService> logger)
    {
        _connStr = connStr;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextSundayNight();
            _logger.LogInformation(
                "[Leaderboard] Next refresh in {Hours:F1} hours (Sunday 14:00 UTC)",
                delay.TotalHours);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await RefreshAsync();
        }
    }

    private static TimeSpan DelayUntilNextSundayNight()
    {
        var now = DateTime.UtcNow;

        // Days until Sunday (0 = today is Sunday)
        int daysUntil = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;

        // If today is Sunday but we're already past 14:00 UTC, wait until next Sunday
        if (daysUntil == 0 && TimeOnly.FromDateTime(now) >= TargetTime)
            daysUntil = 7;

        var next = now.Date.AddDays(daysUntil)
                         .AddHours(TargetTime.Hour)
                         .AddMinutes(TargetTime.Minute);

        return next - now;
    }

    private async Task RefreshAsync()
    {
        try
        {
            _logger.LogInformation("[Leaderboard] Refreshing leaderboards…");
            using var conn = new SqlConnection(_connStr);
            await conn.ExecuteAsync(
                "EXEC dbo.sp_PopulateLeaderboards",
                commandTimeout: 120);
            _logger.LogInformation("[Leaderboard] Swimmer leaderboards refreshed. Refreshing podium leaderboards…");
            await conn.ExecuteAsync(
                "EXEC dbo.sp_PopulatePodiumLeaderboards",
                commandTimeout: 120);
            _logger.LogInformation("[Leaderboard] Refresh completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Leaderboard] Refresh failed.");
        }
    }
}