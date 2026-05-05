using System.Threading.RateLimiting;
using HomeChefPro.Api.Auth;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.Middleware;
using HomeChefPro.Application;
using HomeChefPro.Infrastructure;
using HomeChefPro.Infrastructure.Persistence;
using HomeChefPro.Infrastructure.Uploads;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture));

// ===================================================================
// F-06 (audit Pasada A): validar al startup que NO viaja un password
// de dev en prod. Sin esto, un appsettings.json mal copiado a prod
// expondria 'homechef_dev' literal.
// ===================================================================
if (!builder.Environment.IsDevelopment())
{
    var pgConn = builder.Configuration.GetConnectionString("PostgreSQL") ?? "";
    if (string.IsNullOrWhiteSpace(pgConn))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:PostgreSQL is required in non-Development. " +
            "Set the env var ConnectionStrings__PostgreSQL.");
    }
    if (pgConn.Contains("homechef_dev", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:PostgreSQL contains 'homechef_dev' literal. " +
            "Inject a real password via env var ConnectionStrings__PostgreSQL.");
    }

    // F-13: AllowedHosts no debe ser '*' o vacio en prod.
    var allowedHosts = builder.Configuration["AllowedHosts"] ?? "";
    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
    {
        throw new InvalidOperationException(
            "AllowedHosts must be a specific list (e.g. 'homechef.app;www.homechef.app') in non-Development. " +
            "Wildcard '*' or empty is not allowed.");
    }

    // F-07: bootstrap admin con password de demo se rechaza en prod.
    var bootstrapPwd = builder.Configuration["Bootstrap:Admin:Password"] ?? "";
    if (bootstrapPwd.Equals("demo1234", StringComparison.Ordinal)
        || bootstrapPwd.Equals("admin", StringComparison.OrdinalIgnoreCase)
        || bootstrapPwd.Equals("password", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Bootstrap:Admin:Password is a known demo/default value. " +
            "Generate a strong password before exposing to internet. " +
            "Suggestion: openssl rand -base64 18");
    }
}

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddSingleton<HomeChefPro.Api.Endpoints.DeliveryWebhookSignatureVerifier>();

// ===================================================================
// F-04 (audit Pasada A): rate limiting global por IP.
// - Policy 'auth':   10 req/min — login/register/refresh/change-password
// - Policy 'public': 30 req/min — catalogo, GET orders, receipt PDF, etc.
// - Policy 'upload': 20 req/min — POST payment-proofs (bytes pesados)
// - Policy 'webhook':60 req/min — webhooks de delivery (vienen de proveedores)
// El default global es 60 req/min para no romper UX en navegacion legitima.
// Endpoints autenticados con auth ya tienen otra capa, esto es defensa.
// ===================================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("public", o =>
    {
        o.PermitLimit = 30;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("upload", o =>
    {
        o.PermitLimit = 20;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("webhook", o =>
    {
        o.PermitLimit = 60;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    // Global default por IP para todos los endpoints sin policy explicita.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// CORS: permitir las apps Flutter (web/mobile en dev y dominios prod)
const string CorsPolicyName = "HomeChefCors";
builder.Services.AddCors(options =>
{
    var allowed = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowed.Length > 0)
        {
            policy.WithOrigins(allowed);
        }
        else if (builder.Environment.IsDevelopment())
        {
            // En dev, permitir cualquier puerto de localhost para Flutter web/mobile.
            policy.SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
            });
        }
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

// ===================================================================
// F-05 (audit Pasada A): security headers + HTTPS en non-dev.
// ===================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (ctx, next) =>
{
    // X-Content-Type-Options: navegador respeta el Content-Type sin sniffing.
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    // X-Frame-Options: bloquea iframe embedding (clickjacking).
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    // Referrer-Policy: no enviar referrer cross-origin.
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    // Permissions-Policy: deshabilitar APIs sensibles del browser por default.
    ctx.Response.Headers["Permissions-Policy"] =
        "geolocation=(), camera=(), microphone=(), payment=()";
    await next();
});

app.UseCors(CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// F-02: archivos subidos NO se sirven via UseStaticFiles —
// pasan por endpoints autenticados en UploadEndpoints.
var uploadOpts = app.Services.GetRequiredService<IOptions<LocalFileStorageOptions>>().Value;
Directory.CreateDirectory(uploadOpts.LocalRoot);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Title = "HomeChef Pro API";
        opts.Theme = Scalar.AspNetCore.ScalarTheme.Mars;
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "HomeChefPro.Api" }))
   .WithName("HealthCheck")
   .AllowAnonymous();

// F-12 candidato (queda para Tier 2): /health/db retorna counts del negocio.
// Por ahora se mantiene; reemplazar por /api/admin/dashboard/health en Tier 2.
app.MapGet("/health/db", async (HomeChefProDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);
    if (!canConnect)
        return Results.Problem("Cannot connect to PostgreSQL.", statusCode: 503);
    var ingredientCount = await db.Ingredients.CountAsync(ct);
    var recipeCount = await db.Recipes.CountAsync(ct);
    return Results.Ok(new
    {
        status = "ok",
        db = "postgresql",
        ingredients = ingredientCount,
        recipes = recipeCount,
    });
})
.WithName("DatabaseHealthCheck")
.AllowAnonymous();

app.MapAuthEndpoints();
app.MapAdminIngredientsEndpoints();
app.MapAdminRecipesEndpoints();
app.MapAdminOrdersEndpoints();
app.MapAdminPaymentsEndpoints();
app.MapClientMenuEndpoints();
app.MapClientOrdersEndpoints();
app.MapKitchenEndpoints();
app.MapDeliveryWebhookEndpoints();
app.MapAdminPurchasingEndpoints();
app.MapClientReviewsEndpoints();
app.MapAdminReviewsEndpoints();
app.MapAdminReportsEndpoints();
app.MapKitchenQueueEndpoint();
app.MapAdminInventoryEndpoints();
app.MapUploadEndpoints();
app.MapAdminInvoicesEndpoints();
app.MapClientPreferencesEndpoints();
app.MapClientLoyaltyEndpoints();

// Seed roles (and optional admin) on startup.
var bootstrapEnabled = app.Configuration.GetValue("Bootstrap:EnableOnStart", defaultValue: true);
if (bootstrapEnabled)
{
    try
    {
        await AuthBootstrap.EnsureRolesAndAdminAsync(app.Services);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Auth bootstrap failed (continuing).");
    }
}

app.Run();

public partial class Program; // for WebApplicationFactory
