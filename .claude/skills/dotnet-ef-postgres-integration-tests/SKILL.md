---
name: dotnet-ef-postgres-integration-tests
description: Patrones probados para escribir tests integration con .NET, EF Core y PostgreSQL evitando pitfalls reales (DbUpdateConcurrencyException por collection navigations + triggers, paralelismo de fixtures, JWT issuer mismatch, override de connection strings).
triggers:
  - integration tests with WebApplicationFactory
  - DbUpdateConcurrencyException sin concurrency token
  - tests con Testcontainers Postgres
  - JWT validation falla en tests
  - "expected 1 row affected, but actually affected 0"
---

# Tests integration .NET + EF Core + PostgreSQL: patrones probados

Esta skill condensa lecciones de un proyecto real (HomeChef Pro) donde se pasó de 21 tests rotos a 37/37 verde. Los patrones aplican a cualquier proyecto con la misma stack.

## Cuándo usar

- El proyecto tiene `WebApplicationFactory<Program>` para integration tests.
- La BD es PostgreSQL accedida vía `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Hay triggers SQL (especialmente `BEFORE UPDATE` que tocan `updated_at`).
- Hay autenticación JWT (`AddJwtBearer` con `TokenValidationParameters`).
- Los tests usan Testcontainers para spinear Postgres.

## Anti-patrones que romperán tus tests

### 1. `IClassFixture<TFixture>` con Testcontainers en cada test class

```csharp
// ❌ Esto crea un container Postgres POR CADA clase de test
public class AuthFlowTests : IClassFixture<LiveDatabaseFixture>
public class CatalogFlowTests : IClassFixture<LiveDatabaseFixture>
// ... 11 clases más
```

xunit corre clases de test en paralelo por default. Si hay 11 clases, levantan 11 containers Postgres simultáneamente. El runner de GitHub Actions ubuntu-latest no aguanta y algunos containers fallan al iniciar; los tests reportan errores confusos como "Connection refused 127.0.0.1:5432" porque `_container.GetConnectionString()` cae al puerto default.

**✅ Fix**: collection fixture compartido.

```csharp
// Persistence/IntegrationDbCollection.cs
[CollectionDefinition("IntegrationDb")]
public sealed class IntegrationDbCollection : ICollectionFixture<LiveDatabaseFixture> { }

// En cada test class
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class AuthFlowTests
{
    private readonly LiveDatabaseFixture _fixture;
    public AuthFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;
}
```

Resultado: un solo container Postgres; las clases de la misma collection se ejecutan en serie.

### 2. Override de connection string solo via `AddInMemoryCollection`

```csharp
// ❌ NO siempre gana sobre appsettings.json en todos los caminos de EF
b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new()
{
    ["ConnectionStrings:PostgreSQL"] = _fixture.ConnectionString,
}));
```

Algunos paths (Identity con stores que se resuelven al boot del DI, JWT bearer que captura `Get<JwtOptions>()` en closure) NO ven el override en runtime. El test puede terminar conectándose al `Host=localhost;Port=5432` del `appsettings.json`.

**✅ Fix**: swap directo del `DbContextOptions<TContext>` vía `ConfigureServices`.

```csharp
// Persistence/TestWebAppFactoryExtensions.cs
public static IWebHostBuilder UseTestDatabase(this IWebHostBuilder b, string connectionString)
{
    b.ConfigureServices(services =>
    {
        services.RemoveAll<DbContextOptions<TContext>>();
        services.RemoveAll<DbContextOptions>();
        services.AddDbContext<TContext>(opts => opts.UseNpgsql(connectionString));
    });
    return b;
}
```

Y en el test: `b.UseTestDatabase(_fixture.ConnectionString);`.

### 3. `AddJwtBearer` capturando opciones en closure

```csharp
// En Program.cs
services.AddAppAuthentication(builder.Configuration);

// AuthConfiguration.cs
var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,                          // ← capturado en closure
            IssuerSigningKey = new SymmetricSecurityKey(...),  // ← idem
        };
    });
```

Los valores se capturan al momento de `AddAppAuthentication`. El override del test via `AddInMemoryCollection` puede no aplicar al validator. El token se genera con la clave del test (vía `IOptions<JwtOptions>` que sí es lazy) pero se valida con la del `appsettings.json` → 401 silencioso.

**✅ Fix**: `PostConfigure` que corre después de la registración original.

```csharp
public static IWebHostBuilder UseTestAuth(this IWebHostBuilder b)
{
    const string testIssuer = "Tests-Issuer";
    const string testAudience = "Tests-Audience";
    var testKey = new string('x', 64);

    b.ConfigureServices(services =>
    {
        // Para generar tokens (JwtTokenService usa IOptions<JwtOptions>)
        services.PostConfigure<JwtOptions>(opts =>
        {
            opts.Issuer = testIssuer;
            opts.Audience = testAudience;
            opts.SigningKey = testKey;
        });

        // Para validar tokens (JwtBearer middleware)
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.TokenValidationParameters.ValidIssuer = testIssuer;
                options.TokenValidationParameters.ValidAudience = testAudience;
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(testKey));
            });
    });
    return b;
}
```

### 4. Trigger `BEFORE UPDATE fn_touch_updated_at` + `Touch()` en C#

Si Postgres tiene un trigger:
```sql
CREATE FUNCTION fn_touch_updated_at() RETURNS TRIGGER AS $$
BEGIN NEW.updated_at := NOW(); RETURN NEW; END;
$$ LANGUAGE plpgsql;
CREATE TRIGGER trg_xxx BEFORE UPDATE ON xxx FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();
```

Y el Domain también toca `UpdatedAt`:
```csharp
public void Activate(TimeProvider? clock) { IsActive = true; Touch(clock); }
private void Touch(TimeProvider? clock) => UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
```

EF emite `UPDATE x SET updated_at = @p, ...`. El trigger lo sobrescribe con `NOW()`. Por la interacción con `OUTPUT/RETURNING` que usa Npgsql para tracking, EF reporta "0 rows affected" → `DbUpdateConcurrencyException`.

**✅ Fix**: marcar `UpdatedAt` como gestionado por la BD.

```csharp
// EntityConfiguration.cs
builder.Property(x => x.UpdatedAt)
    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
```

`Ignore` significa: si C# cambia el valor, EF NO lo incluye en el UPDATE. Combinado con eliminar el `Touch()` del método del Domain cuando solo se modifica un hijo de la collection (ver siguiente).

### 5. `Include + Add to collection navigation + SaveChanges`

```csharp
// ❌ Patrón tóxico
public async Task<Guid> Handle(AddPresentationCommand cmd, CancellationToken ct)
{
    var ingredient = await db.Ingredients
        .Include(i => i.Presentations)
        .FirstAsync(i => i.Id == cmd.IngredientId, ct);

    var presentation = ingredient.AddPresentation(...);  // _presentations.Add(p)

    await db.SaveChangesAsync(ct);  // ← DbUpdateConcurrencyException
    return presentation.Id;
}
```

Aunque solo se agregue un hijo nuevo, EF marca al parent como Modified. Sin columnas escalares cambiadas, EF emite un UPDATE vacío que el trigger BEFORE UPDATE rechaza con 0 rows → exception.

**✅ Fix**: bypassar el tracking del parent.

```csharp
public async Task<Guid> Handle(AddPresentationCommand cmd, CancellationToken ct)
{
    var exists = await db.Ingredients
        .AsNoTracking()
        .AnyAsync(i => i.Id == cmd.IngredientId, ct);
    if (!exists) throw new NotFoundException(...);

    // Validación de duplicado (sin cargar el parent con tracking)
    var dup = await db.IngredientPresentations
        .AsNoTracking()
        .AnyAsync(p => p.IngredientId == cmd.IngredientId && p.Name == cmd.Name, ct);
    if (dup) throw new DomainException(...);

    // Crear el hijo standalone
    var presentation = IngredientPresentation.Create(
        ingredientId: cmd.IngredientId,
        name: cmd.Name,
        ...);

    db.IngredientPresentations.Add(presentation);  // ← directo al DbSet
    await db.SaveChangesAsync(ct);  // → solo INSERT, no UPDATE
    return presentation.Id;
}
```

Requiere que el factory method del Domain (`Create`) sea `public` en lugar de `internal`.

## Pattern completo del test class

```csharp
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class AuthFlowTests
{
    private readonly LiveDatabaseFixture _fixture;
    public AuthFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> CreateApi() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestDatabase(_fixture.ConnectionString);
            b.UseTestAuth();
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new()
            {
                ["Bootstrap:EnableOnStart"] = "false",
            }));
        });

    [Fact]
    public async Task Registers_user()
    {
        await using var factory = CreateApi();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            Email: $"u-{Guid.NewGuid():N}@example.com",
            Password: "Test1234",
            FullName: "Tester"));

        // SIEMPRE leer el body en errores para que el test failure sea diagnosticable
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected 201, got {response.StatusCode}. Body: {body}");
        }
    }
}
```

## Checklist antes de pushear cambios a tests integration

1. ✅ Build local pasa: `dotnet build --configuration Release`
2. ✅ Tests pasan en local con Docker corriendo: `dotnet test --filter Category=Integration`
3. ✅ El test que escribiste corre AISLADAMENTE: `dotnet test --filter "FullyQualifiedName~MiTest"`
4. ✅ El handler que tocaste tiene su pattern correcto (AsNoTracking si solo agrega hijo)
5. ✅ Si tocaste el modelo EF, ¿`UpdatedAt` tiene `SetAfterSaveBehavior(Ignore)`?
6. ✅ Si tocaste auth, ¿el test usa `UseTestAuth()`?

## Errores comunes y diagnóstico

| Síntoma | Causa probable | Fix |
|---|---|---|
| `Connection refused 127.0.0.1:5432` en algunos tests, otros pasan | `IClassFixture` paralelo + container falla | Migrar a `[Collection]` |
| `Connection refused 5432` en TODOS los tests | Override del `DbContextOptions` no aplica | Usar `UseTestDatabase()` ext |
| `DbUpdateConcurrencyException: expected 1 row, but 0` en handler con collection | EF emite UPDATE vacío del parent | `AsNoTracking + db.<DbSet>.Add()` |
| `JsonException: input does not contain JSON tokens` después de POST | El endpoint devolvió 401 con body vacío | Verificar JWT con `UseTestAuth()` |
| Tests pasan local, fallan en CI | Múltiples containers paralelos colapsan | `[Collection("IntegrationDb")]` |

## Cómo enriquecer el ExceptionHandlingMiddleware para diagnóstico

```csharp
catch (Exception ex)
{
    string detail;
    if (env.IsDevelopment())
    {
        var sb = new StringBuilder();
        var current = ex;
        var depth = 0;
        while (current is not null && depth < 5)
        {
            if (depth > 0) sb.Append(" --> ");
            sb.Append(current.GetType().Name).Append(": ").Append(current.Message);
            current = current.InnerException;
            depth++;
        }
        detail = sb.ToString();
    }
    else
    {
        detail = "An unexpected error occurred.";
    }
    // ...
}
```

Sin esto, el `detail` solo dice `"InvalidOperationException: An exception has been raised that is likely due to a transient failure"` (el wrapper EF) y oculta la `NpgsqlException` real con el mensaje crítico (FK violation, missing column, etc.).
