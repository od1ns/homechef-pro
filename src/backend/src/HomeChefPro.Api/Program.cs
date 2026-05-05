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

    // F-07: bootstrap admin con password de demo o placeholder se rechaza en prod.
    // Lista incluye los demos clasicos + placeholders del .env.example
    // (descubiertos en Fase 6-B staging local: el operador puede pegar el
    // example sin reemplazar y el password "GenerarConOpensslRandBase64_18"
    // pasa F-07 trivialmente).
    var bootstrapPwd = builder.Configuration["Bootstrap:Admin:Password"] ?? "";
    var lowerPwd = bootstrapPwd.ToLowerInvariant();
    string[] forbiddenLiterals =
    {
        "demo1234", "admin", "password", "12345678",
    };
    string[] forbiddenSubstrings =
    {
        "generarcon",      // placeholder del .env.staging.local.example
        "change-me", "changeme",
        "replace-me", "replaceme",
        "your-password", "yourpassword",
        "placeholder",
        "example",
    };
    if (forbiddenLiterals.Any(p => lowerPwd.Equals(p, StringComparison.Ordinal))
        || forbiddenSubstrings.Any(s => lowerPwd.Contains(s, StringComparison.Ordinal)))
    {
        throw new InvalidOperationException(
            "Bootstrap:Admin:Password is a known demo/placeholder value. " +
            "Generate a strong password before exposing to internet. " +
            "Suggestion: openssl rand -base64 18");
    }
    // Min length 12 para hacer brute-force impractico.
    if (bootstrapPwd.Length < 12)
    {
        throw new InvalidOperationException(
            "Bootstrap:Admin:Password must be at least 12 chars long.");
    }
}

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddSingleton<HomeChefPro.Api.Endpoints.DeliveryWebhookSignatureVerifier>();

// ===================================================================
// F-04 (Pasada A) + F-28 (Tier 2): rate limiting particionado por IP.
// - Policy 'auth':   10 req/min/IP — login/register/refresh/change-password
// - Policy 'public': 30 req/min/IP — catalogo, GET orders anon, receipt PDF
// - Policy 'upload': 20 req/min/IP — POST payment-proofs (bytes pesados)
// - Policy 'webhook':60 req/min/IP — webhooks de delivery (vienen de proveedores)
// - Global:          120 req/min/IP (default para todo lo demas)
// Tests setean RateLimiting:Disabled=true para subir limites a int.MaxValue.
// ===================================================================
var rateLimitingDisabled = builder.Configuration.GetValue("RateLimiting:Disabled", defaultValue: false);
int AuthLimit    = rateLimitingDisabled ? int.MaxValue : 10;
int PublicLimit  = rateLimitingDisabled ? int.MaxValue : 30;
int UploadLimit  = rateLimitingDisabled ? int.MaxValue : 20;
int WebhookLimit = rateLimitingDisabled ? int.MaxValue : 60;
int GlobalLimit  = rateLimitingDisabled ? int.MaxValue : 120;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static RateLimitPartition<string> PerIpPartition(HttpContext ctx, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });

    options.AddPolicy("auth",    ctx => PerIpPartition(ctx, AuthLimit));
    options.AddPolicy("public",  ctx => PerIpPartition(ctx, PublicLimit));
    options.AddPolicy("upload",  ctx => PerIpPartition(ctx, UploadLimit));
    options.AddPolicy("webhook", ctx => PerIpPartition(ctx, WebhookLimit));

    // Global default por IP para todos los endpoints sin policy explicita.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        ctx => PerIpPartition(ctx, GlobalLimit));
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

// F-12 (Tier 2 cerrado): health check minimal — no leak counts del negocio.
// Probes solo confirman connectividad; los counts viven en /api/admin/dashboard
// que requiere rol Admin.
app.MapGet("/health/db", async (HomeChefProDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);
    if (!canConnect)
        return Results.Problem("Cannot connect to PostgreSQL.", statusCode: 503);
    return Results.Ok(new { status = "ok", db = "postgresql" });
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
