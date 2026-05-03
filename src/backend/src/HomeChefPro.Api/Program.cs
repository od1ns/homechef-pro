using HomeChefPro.Api.Auth;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.Middleware;
using HomeChefPro.Application;
using HomeChefPro.Infrastructure;
using HomeChefPro.Infrastructure.Persistence;
using HomeChefPro.Infrastructure.Uploads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture));

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddSingleton<HomeChefPro.Api.Endpoints.DeliveryWebhookSignatureVerifier>();

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
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

// Serve uploaded files from a known root so the URLs returned by IFileStorage are real.
var uploadOpts = app.Services.GetRequiredService<IOptions<LocalFileStorageOptions>>().Value;
Directory.CreateDirectory(uploadOpts.LocalRoot);
var uploadsRoute = uploadOpts.PublicBaseUrl.StartsWith('/') ? uploadOpts.PublicBaseUrl : "/uploads";
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.GetFullPath(uploadOpts.LocalRoot)),
    RequestPath = uploadsRoute.TrimEnd('/'),
});

if (app.Environment.IsDevelopment())
{
    // OpenAPI raw (JSON) en /openapi/v1.json
    app.MapOpenApi();

    // Scalar UI: documentacion interactiva de la API en /scalar/v1
    // (mas moderna y rapida que Swagger UI). Solo en dev.
    app.MapScalarApiReference(opts =>
    {
        opts.Title = "HomeChef Pro API";
        opts.Theme = Scalar.AspNetCore.ScalarTheme.Mars;
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "HomeChefPro.Api" }))
   .WithName("HealthCheck")
   .AllowAnonymous();

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
