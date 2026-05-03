# API HomeChef Pro

REST API .NET 10 con MediatR + EF Core. JWT Bearer + refresh tokens.
**60+ endpoints** agrupados por área funcional.

## Documentación interactiva (dev)

Cuando el backend corre en modo Development (default en
`docker-compose.yml`), tenés dos vistas auto-generadas:

| URL | Qué |
|---|---|
| `http://localhost:8080/openapi/v1.json` | Spec OpenAPI 3.0 raw (JSON). Usable en Postman, Insomnia, Bruno, codegen. |
| `http://localhost:8080/scalar/v1` | UI interactiva tipo Swagger pero más rápida (Scalar). Probá endpoints desde el navegador. |

## Importar en Postman

```
Postman → Import → Link → http://localhost:8080/openapi/v1.json
```

Postman crea una collection con todos los endpoints + sus schemas. Para
que funcionen los endpoints autenticados:

1. Postman → Environments → New → poné variables:
   - `baseUrl` = `http://localhost:8080`
   - `accessToken` = (lo pegás después del login)
2. En la collection, Authorization → Bearer Token → `{{accessToken}}`.
3. Hacé un POST a `/api/auth/login` con `admin@homechef.local` / `demo1234`.
4. Copiá el `accessToken` del response y pegalo en el environment.

Para flujos que rotan el JWT cada 60 min, usá el endpoint
`POST /api/auth/refresh` con `{ "refreshToken": "..." }` cuando expire.

## Mapa de endpoints

### Auth (`/api/auth`)
- `POST /register` — crear cliente nuevo (rol Client por default).
- `POST /login` — JWT + refresh token.
- `POST /refresh` — rotar par de tokens (revoca el viejo).
- `POST /logout` — revoca el refresh token actual.
- `POST /change-password` — requiere JWT.
- `GET  /me` — perfil del usuario autenticado.

### Cliente (`/api/client`)
- `GET  /menu` — catálogo público (sin auth).
- `GET  /menu/{id}` — detalle de plato.
- `POST /orders` — crear orden (acepta guest o autenticado).
- `GET  /orders/{id}` — tracking público por id.
- `POST /orders/{id}/payment` — adjuntar comprobante.
- `GET  /orders/{id}/receipt.pdf` — recibo PDF.
- `GET  /me/preferences`, `PUT /me/preferences` — onboarding sync.
- `GET  /loyalty/me` — saldo + nivel + puntos al siguiente nivel.
- `GET  /loyalty/rewards` — catálogo de recompensas activas.
- `POST /loyalty/redeem/{rewardId}` — canjear recompensa.
- `GET  /reviews/dish/{dishId}` — reseñas públicas.
- `GET  /me/reviews` — mis reseñas.
- `POST /reviews` — dejar reseña.
- `PATCH /reviews/{id}` — editar reseña propia.

### Admin (`/api/admin`)
Todos requieren rol `Admin`.

- **Insumos**: `GET/POST /ingredients`, presentations, thresholds, deactivate.
- **Recetas**: `GET/POST /recipes`, components, costo, out-of-stock.
- **Inventario**: `POST /inventory/purchases`, `POST /inventory/waste`.
- **Compras**: `GET /purchasing/forecast`.
- **Órdenes**: list, get, advance, receipt.pdf.
- **Pagos**: pending, verify, reject.
- **Facturas**: list, get, emit, cancel.
- **Reseñas**: list, hide.
- **Reportes**: dish-margin, recipe-costs, reorder-suggestions, sales-daily,
  inventory-rotation, peak-hours-heatmap, peak-hours-summary, customer-ranking.

### Cocina (`/api/kitchen`)
Requiere rol `Admin` o `Cook`.

- `GET /orders` — órdenes activas.
- `GET /queue` — cola enriquecida con prep time + procedimiento.
- `POST /orders/{id}/items/{itemId}/start` — iniciar preparación.
- `POST /orders/{id}/items/{itemId}/ready` — marcar listo.

### Webhooks (`/api/webhooks`)
- `POST /delivery/{provider}` — eventos de delivery.
  Verifica HMAC-SHA256 contra `DeliveryWebhooks:Secrets:{provider}`
  (sin secret = no se valida; con secret y firma incorrecta = 401).

### Uploads (`/api/uploads`)
- `POST /payment-proofs` — multipart, image/png|jpg|webp.

### Health
- `GET /health` — pulso básico.
- `GET /health/db` — pulso + count de ingredientes y recetas.

## Convenciones

- **Errores**: ProblemDetails (RFC 7807) con `type`, `title`, `status`, `detail`,
  `instance`. Validación adicional incluye `errors` con campos.
- **Status codes**: 200 (OK), 201 (Created), 204 (No content), 400 (validación),
  401 (no auth/token expirado), 403 (auth pero sin rol), 404 (no encontrado),
  409 (regla de dominio violada), 500 (excepción no manejada).
- **Naming**: camelCase en JSON (Pascal en C# se mapea automáticamente).
- **Money**: `decimal` con `numeric(N,M)` en SQL para evitar `OverflowException`.
- **Fechas**: `DateTimeOffset` ISO 8601, siempre UTC en la BD (Npgsql exige offset 0).

## Refrescar la documentación

Cualquier cambio en el código (nuevo endpoint, DTO, etc.) refleja
automáticamente en `/openapi/v1.json` en el próximo arranque del backend
(`MapOpenApi` introspecta el endpoint tree). Para regenerar la collection
de Postman, re-importá el JSON.
