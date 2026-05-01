# Frontend Flutter — HomeChef Pro

Tres apps independientes que consumen el mismo backend `.NET` REST y comparten
tema, i18n y cliente HTTP a través del paquete `shared/`.

```
src/frontend/
├── shared/           # paquete reutilizable (theme, i18n, api client, models)
├── client_app/       # app móvil para clientes (iOS + Android)
├── admin_web/        # web responsive para el chef
└── kitchen_tablet/   # tablet de cocina (Android tablet en horizontal)
```

## Setup local

1. Instalar Flutter SDK 3.24 o superior:
   <https://docs.flutter.dev/get-started/install>

2. Verificar:
   ```bash
   flutter --version
   flutter doctor
   ```

3. Resolver dependencias del paquete shared y de cada app:
   ```bash
   cd src/frontend/shared       && flutter pub get
   cd ../client_app             && flutter pub get
   cd ../admin_web              && flutter pub get
   cd ../kitchen_tablet         && flutter pub get
   ```

## Correr el backend

```bash
cd deploy && docker compose up -d postgres redis
dotnet run --project src/backend/src/HomeChefPro.Api
# La API escucha en http://localhost:5000 por defecto.
```

## Correr la app cliente

```bash
cd src/frontend/client_app
flutter run --dart-define=HCP_API_BASE=http://localhost:5000
# En emulador Android, `localhost` apunta al emulador, no al host.
# Para el emulador Android usa: HCP_API_BASE=http://10.0.2.2:5000
# Para iOS simulator: http://localhost:5000 funciona.
```

## Estado actual

| App | Pantallas implementadas | Notas |
|---|---|---|
| `shared/` | tema (4 paletas), i18n es/en, ApiClient + AuthStorage, `LocalOrderStore` (shared_preferences), modelos `RecipeSummary`/`Recipe`/`Order`/`OrderItem`/`KitchenQueueItem`/`PublicReview`, `HcpApi.{login,register,menu,dish,dishReviews,createGuestOrder,trackOrder,kitchenQueue,startItem,markItemReady}` | listo |
| `client_app/` | C1 catálogo · C2 detalle · C3 cart + checkout · C4 tracking timeline · C5 **Reviews tab** (login → mis reseñas + pedidos por reseñar + leave/edit con star rating) · C6 perfil con login/logout y preferencias · O1–O4 onboarding · **PaymentScreen** con image_picker + multipart de comprobante · **Login/Register tabs** | ★ feature-completo |
| `admin_web/` | A1 Resumen · A2 Live Orders · A3 Recipe Editor · A4 Inventario (compras + mermas) · A6 Analytics · A7 Facturas con **descarga real del PDF en web** · S1 Scale-to-Demand | Ajustes pendiente |
| `kitchen_tablet/` | **Login + queue 2-columnas (pending / in_prep) con polling 15s + start/ready + procedimiento collapsible** | ★ funcional end-to-end |

## Convenciones

- **Tema**: cualquier widget puede leer la paleta completa con
  `Theme.of(context).extension<HcpThemeExtension>()!.palette`. Switch del tema
  vía `AppState.setTheme(...)`.
- **i18n**: por ahora un diccionario `Map<String, String>` con claves estables
  (`tab.browse`, `cart.placeOrder`, …). Cuando crezca, migrar a
  `flutter_localizations` + `arb` files.
- **Auth**: el `AuthStorage` guarda el JWT en keychain/keystore. El
  `ApiClient` lo agrega como `Authorization: Bearer …` automáticamente.
- **Errores**: `ApiException(statusCode, message, body)` lanzada por el
  cliente. Los handlers del backend devuelven `ProblemDetails` JSON con
  `detail` que se usa como mensaje user-friendly.

## Roadmap (en orden de prioridad)

1. SENIAT/IGTF integración real para A7.
2. Migrar i18n a `flutter_localizations` + `arb` files.
3. Cuando se defina el producto Sabor: endpoint backend de loyalty + UI de balance/canje en client_app.
4. Subir las preferencias del onboarding al backend cuando exista endpoint `customer_profile_extended` (hoy viven solo en shared_preferences).
5. Migrar storage de imágenes a S3/B2 cuando crezca el volumen (hoy filesystem local detrás de `/uploads/*`).
6. Mobile/desktop download del PDF (hoy sólo web; se podría añadir vía `path_provider` + `share_plus`).

## Cómo correr el chef-tablet

```bash
cd src/frontend/kitchen_tablet
flutter run --dart-define=HCP_API_BASE=http://10.0.2.2:5000
# Login: usar el admin bootstrap o crear un usuario con rol "Cook" desde admin_web (cuando exista).
```

## Cómo correr el admin web

```bash
cd src/frontend/admin_web
flutter run -d chrome --dart-define=HCP_API_BASE=http://localhost:5000
# Login con el admin del bootstrap (Bootstrap:Admin:Email/Password en appsettings).
```
