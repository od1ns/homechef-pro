# HomeChef Pro — Resumen de integración

Este archivo es la referencia rápida para asistentes que entran al proyecto. Resume estado, convenciones, decisiones y patrones que ya están establecidos. **Leer esto ANTES de proponer cambios.**

> Para reglas de comunicación (idioma, tono, etc.) ver `CLAUDE.md`.

## 1. Stack

| Capa | Tech |
|---|---|
| Backend | .NET 10, Clean Architecture (Domain / Application / Infrastructure / Api) |
| ORM | EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL 10 |
| Mediator | MediatR + FluentValidation + AutoMapper |
| BD | PostgreSQL 16 (esquema en SQL puro, snake_case, no usa EF Migrations — `EnsureCreated` ad-hoc para Identity) |
| Cache | Redis 7 (poco usado actualmente) |
| Auth | ASP.NET Identity + JWT Bearer + refresh tokens con rotación y reuse detection |
| PDF | QuestPDF para facturas/recibos |
| Frontend | Flutter 3.41 (CanvasKit por default), 3 apps: `admin_web` (8090), `client_app` (8091), `kitchen_tablet` (8092), todas comparten `shared/` |
| Deploy | Docker Compose (postgres + redis + api + nginx + certbot) |
| Tests | xunit + Testcontainers para integration; Playwright para E2E (limitado por CanvasKit) |

## 2. Estructura del repo

```
HomeChef Pro/
├── CLAUDE.md                 # Reglas de comunicación
├── INTEGRACION.md            # Este archivo
├── deploy/                   # docker-compose, nginx, certbot
├── docs/                     # API.md, DEPLOY.md
├── scripts/                  # smoke.ps1, smoke-deep.ps1, seed-*.ps1
├── src/
│   ├── backend/
│   │   ├── HomeChefPro.slnx
│   │   ├── src/
│   │   │   ├── HomeChefPro.Domain/         # Entities, ValueObjects, DomainExceptions
│   │   │   ├── HomeChefPro.Application/    # Commands/Queries (MediatR), Behaviors (Validation/Logging)
│   │   │   ├── HomeChefPro.Infrastructure/ # DbContext, Configurations, Identity, JWT
│   │   │   └── HomeChefPro.Api/            # Endpoints minimales, Middleware, Auth
│   │   └── tests/
│   │       ├── HomeChefPro.Domain.Tests/
│   │       ├── HomeChefPro.Application.Tests/
│   │       └── HomeChefPro.Api.IntegrationTests/  # 37/37 verde
│   ├── database/
│   │   ├── schema/           # SQL puro, ordenados por prefijo numérico (00_, 01_, ...)
│   │   └── seed/             # Seeds opcionales
│   └── frontend/
│       ├── shared/           # Modelos Dart, ApiClient, AuthStorage, i18n, theme
│       ├── admin_web/        # Flutter web puerto 8090
│       ├── client_app/       # Flutter web/mobile puerto 8091
│       └── kitchen_tablet/   # Flutter tablet puerto 8092
├── tests/
│   └── e2e/                  # Playwright (health checks pasan; UI tests skipped por CanvasKit)
└── .github/workflows/ci.yml  # Build + integration tests (Testcontainers)
```

## 3. Convenciones

### SQL
- **Todo snake_case**: tablas, columnas, vistas, funciones, índices.
- **7 tablas de Identity también en snake_case** (`asp_net_users`, etc.) — ver `01a_identity_tables.sql`.
- **Vistas analíticas** en `10_views.sql`, **triggers** en `11_functions_triggers.sql`.
- **Cast a `numeric(N,M)` obligatorio en raw SQL** que se proyecta a `decimal` en C# — algunos cálculos (price/cost ratios) generan numerics con > 28 dígitos significativos que rompen el cast con `OverflowException`.

### EF Core ↔ Postgres
- **`UpdatedAt` es manejado por trigger SQL**, NO por C#. En las configuraciones de entidades con `updated_at` y trigger `BEFORE UPDATE fn_touch_updated_at`, marcar:
  ```csharp
  builder.Property(x => x.UpdatedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
  ```
  Aplicado en: Ingredient, IngredientPresentation, Recipe, Order (parcial), UserProfile, CustomerPreferences, DeliveryTracking, Invoice, Review.

- **NO llamar `Touch()` en Domain cuando solo se agrega un hijo a una collection** (`_presentations.Add(...)`). EF marca al parent como Modified y emite UPDATE vacío que rompe con triggers (DbUpdateConcurrencyException). El trigger SQL ya se encarga.

- **Para handlers que solo agregan un hijo standalone**, usar `AsNoTracking()` para verificar el parent y `db.<DbSet>.Add()` directo:
  ```csharp
  // ❌ NO hacer
  var ingredient = await db.Ingredients.Include(i => i.Presentations).FirstAsync(...);
  ingredient.AddPresentation(...);
  await db.SaveChangesAsync(); // → DbUpdateConcurrencyException

  // ✅ Hacer
  var exists = await db.Ingredients.AsNoTracking().AnyAsync(i => i.Id == id, ct);
  if (!exists) throw new NotFoundException(...);
  var presentation = IngredientPresentation.Create(...);
  db.IngredientPresentations.Add(presentation);
  await db.SaveChangesAsync();
  ```

### Auth
- JWT issuer/audience/key se leen en `AuthConfiguration.AddAppAuthentication` en TIEMPO DE REGISTRO (capturados en closure). Los tests deben usar `PostConfigure<JwtBearerOptions>` para sobrescribir, no solo `ConfigureAppConfiguration`.

### Money / fechas
- **Money**: `decimal` en C# con `numeric(N,M)` en SQL.
- **Fechas**: `DateTimeOffset` ISO 8601, **siempre UTC en BD** (Npgsql exige offset 0). En triggers de loyalty/orders, normalizar zonas Caracas a UTC con `ToUniversalTime()`.

### API / errores
- **ProblemDetails** (RFC 7807) con `type`, `title`, `status`, `detail`, `instance`. En Development el `detail` incluye toda la cadena de inner exceptions (`ExceptionHandlingMiddleware` enriquecido).
- **Códigos**: 200/201/204 OK, 400 validación, 401 no auth, 403 sin rol, 404 no encontrado, 409 regla de dominio violada, 500 excepción no manejada.

### CI
- **`.editorconfig` suprime warnings de estilo** (CA1707/CA1725/CA1848/CA1873/CA1711/CA1304/CA1311/CA1862) con justificación documentada. NO arreglar uno por uno; si aparece nuevo CA, evaluar si es bug real o estilo.
- **Integration tests sin `continue-on-error`** — bloquean PRs.

## 4. Tests integration: setup que evita los pitfalls

Todas las clases con `[Trait("Category", "Integration")]` usan `[Collection("IntegrationDb")]` que comparte un solo `LiveDatabaseFixture` (un container Postgres). Y dos extensiones helpers en `Persistence/`:

- **`UseTestDatabase(connectionString)`** — swap del `DbContextOptions<HomeChefProDbContext>` vía `ConfigureServices` post Program.cs. Necesario porque `AddInMemoryCollection` solo no gana sobre `appsettings.json` en todos los caminos.
- **`UseTestAuth()`** — `PostConfigure<JwtOptions>` y `PostConfigure<JwtBearerOptions>` con valores fijos. Así generación y validación usan la misma clave.

Pattern del `CreateApi()` de cada test:
```csharp
new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
{
    b.UseEnvironment("Development");
    b.UseTestDatabase(_fixture.ConnectionString);
    b.UseTestAuth();
    b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(...));
});
```

## 5. Frontend Flutter

### Limitación: Flutter web 3.41 con CanvasKit
- Pinta todo en `<canvas>` → Playwright no encuentra el DOM.
- Tests UI E2E están **skipped** con justificación. Para reactivarlos hay que migrar a `--web-renderer=html` (deprecated 3.13+) o a `flutter drive` + `integration_test`.
- Cobertura E2E real está en: integration tests xunit (37/37), `smoke.ps1`, `smoke-deep.ps1`, health checks Playwright.

### Patrón de pantallas
- `client_app/lib/screens/*_screen.dart` — `StatefulWidget` que recibe `AppState` (ChangeNotifier).
- I18n con `state.strings.t('key')`. Strings en `shared/lib/i18n/strings.dart` (mapas es/en).
- API calls vía `state.api.<methodName>()` (HcpApi en shared).
- Storage de tokens en `shared/lib/api/auth_storage.dart` con `flutter_secure_storage`.

## 6. Estado actual

### Tests
- **Integration**: 37/37 verde
- **Unit**: pasan
- **Domain**: pasan
- **E2E health**: 4/4 verde
- **E2E UI**: skipped (limitación CanvasKit)

### Features completas
- Auth: register, login, refresh, logout, change-password, /me.
- Catálogo público: menú, dish detail, reviews.
- Orders: guest + registered, payment proof, kitchen flow, delivery webhooks (HMAC verificado), receipt PDF.
- Invoicing: SENIAT/IGTF, mock provider en dev.
- Inventario: ingredientes, presentaciones, compras (trigger actualiza stock + avg cost), waste.
- Compras: forecast con consumo histórico.
- Reportes: dish-margin, recipe-costs, reorder-suggestions, sales-daily, **inventory-rotation**, **peak-hours-heatmap**, **peak-hours-summary**, **customer-ranking** (RFM).
- **Sabor (loyalty)**: trigger acredita al `delivered`, niveles bronce/plata/oro, catálogo de rewards, redeem. UI cliente completa.
- HTTPS con certbot configurado (no desplegado todavía).
- Refresh tokens con rotación.

### Features pendientes
- **Admin web**: pantalla de Ajustes (placeholder), panel admin de Sabor (CRUD rewards, transacciones).
- Notificaciones push, sistema de cupones, caching Redis efectivo, dashboard ejecutivo.
- Deploy real a producción.

### Issues conocidos
- Flutter web tests UI: skipped por CanvasKit. Plan documentado en specs Playwright.
- Tests integration en CI: usan `LiveDatabaseFixture` con un solo container compartido. Si se reintroduce `IClassFixture<LiveDatabaseFixture>` en una clase, vuelven los timeouts paralelos.

## 7. Lecciones que NO repetir

Errores cometidos en sesiones previas y sus respectivas mitigaciones:

| Error | Mitigación |
|---|---|
| `Edit` tool truncando archivos cuyo path tiene espacios (caso de "HomeChef Pro/") | Usar `bash` con heredoc `<< 'EOF'` o `Python` para escribir archivos largos. Verificar siempre con `wc -l` + brace count tras cada edición. |
| Especular y pushear sin verificar (16+ rondas de "push → CI rojo → revertir") | Antes de pushear: `dotnet build` local; para fixes de SQL/EF capturar el SQL real con logging; para tests UI escribir prueba mínima aislada antes de escalar. |
| Reintroducir patrones ya descartados (los specs Playwright UI con click en `flt-semantics-placeholder` que ya estaban skipped con la nota explicando por qué) | Antes de implementar: leer si ya hay docs/comentarios/tests skipped sobre el mismo problema. |
| Asumir que un package está disponible (`Microsoft.AspNetCore.TestHost.ConfigureTestServices` cuando solo se referencia `Mvc.Testing`) | `grep` en el `.csproj` antes de usar APIs nuevas. |
| Marcar entidad como `EntityState.Unchanged` cuando el problema real era `Include + Add` en la collection navigation | Ver pattern de `AsNoTracking + Add` en sección 3. |
| Olvidar instrucción de "español neutro" en sesiones largas | `CLAUDE.md` debe estar visible al inicio de cada respuesta; releer si la conversación pasa los ~30 mensajes. |
| Pushear cambios "diagnósticos" que se acumulan en historial | Usar `git commit --amend` para fixes que solo enriquecen errores; mejor 1 commit limpio que 5 ruidosos. |

## 8. Cómo leer los logs de CI sin SSH

Las metadata de runs son públicas vía API GitHub:
- `GET /repos/{owner}/{repo}/actions/runs/{run_id}` → status del workflow
- `GET /repos/{owner}/{repo}/actions/runs/{run_id}/artifacts` → URLs de artifacts

Los logs raw requieren admin auth (no funcionan sin token). Para leer resultados de tests:
1. El workflow debe subir el `.trx` como artifact (`actions/upload-artifact@v4`).
2. El usuario descarga el zip vía la URL pública del artifact (clic en GitHub Actions UI estando logueado).
3. El asistente parsea el `.trx` con Python (`xml.etree.ElementTree`).

Si GitHub rate-limita las llamadas API anónimas, esperar 60s o pedir al usuario que descargue el artifact directo.

## 9. Credenciales y comandos rápidos

```
Admin:  admin@homechef.local / demo1234
Cliente smoke: maria@example.com / demo1234

Smoke API:                pwsh ./scripts/smoke.ps1
Smoke flujo completo:     pwsh ./scripts/smoke-deep.ps1
Seed compras:             pwsh ./scripts/seed-purchases.ps1
Seed historia rica:       pwsh ./scripts/seed-rich-history.ps1
Backend up:               cd deploy && docker compose up -d
Build local:              dotnet build src/backend/HomeChefPro.slnx -c Release
Tests integration:        dotnet test src/backend/HomeChefPro.slnx --filter Category=Integration
Tests unit:               dotnet test src/backend/HomeChefPro.slnx --filter "Category!=Integration"
```

OpenAPI dev: `http://localhost:8080/openapi/v1.json`
Scalar UI: `http://localhost:8080/scalar/v1`
