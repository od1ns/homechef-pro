# Smoke test del backend HomeChef Pro

Plan paso-a-paso para validar que los **40 endpoints** funcionan después de
levantar Docker, sin necesidad de Flutter. Cada paso incluye el `curl` listo
para copiar+pegar y el resultado esperado.

> Pre-requisito: backend corriendo en `http://localhost:8080`. Ver `docs/DEPLOY.md` §1–§4 para Docker, o `deploy/docker-compose.yml` para dev local.

## 0. Preparar bootstrap admin

El admin inicial solo se crea si `Bootstrap:Admin:Email` y `Bootstrap:Admin:Password` están en config. **Por defecto el `docker-compose.yml` no los pasa** — agregalos antes del primer `up`:

```yaml
# deploy/docker-compose.yml — sección api.environment
Bootstrap__Admin__Email: admin@homechef.local
Bootstrap__Admin__Password: ChangeMe123!
Bootstrap__Admin__FullName: "Admin Inicial"
```

Reiniciar `docker compose up -d api` y mirar los logs por `EnsureRolesAndAdminAsync`. Si la BD ya tenía un admin previo, el bootstrap no hace nada.

## 1. Health check

```bash
curl -i http://localhost:8080/health
# 200 OK · {"status":"healthy"}

curl -i http://localhost:8080/health/db
# 200 OK · valida que Postgres responde
```

## 2. Login admin → guardar JWT

```bash
ADMIN_TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@homechef.local","password":"ChangeMe123!"}' \
  | jq -r .accessToken)
echo "$ADMIN_TOKEN" | head -c 40
```

Si `jq` no está, el response es:

```json
{"userId":"...","email":"admin@homechef.local","fullName":"Admin Inicial","roles":["Admin"],"accessToken":"eyJhbGc...","expiresAt":"..."}
```

## 3. GET /api/auth/me

```bash
curl -s http://localhost:8080/api/auth/me \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq
# { userId, email, fullName, roles:["Admin"] }
```

## 4. Catálogo: crear insumo (harina de maíz)

```bash
HARINA_ID=$(curl -s -X POST http://localhost:8080/api/admin/ingredients \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Harina de maíz precocida",
    "useUnit":"g",
    "reorderPointUseUnit":5000,
    "minimumStockUseUnit":1000,
    "description":"Marca P.A.N. - blanca"
  }' | jq -r .id)
echo "harina=$HARINA_ID"
# 201 Created
```

## 5. Listar insumos + detail

```bash
curl -s "http://localhost:8080/api/admin/ingredients?onlyActive=true" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq '.[0]'

curl -s "http://localhost:8080/api/admin/ingredients/$HARINA_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq
# stockUseUnit:0, avgCostPerUseUnitUsd:0 (hasta primera compra)
```

## 6. Agregar presentación de compra (saco 1 kg = 1000 g)

```bash
PRES_ID=$(curl -s -X POST "http://localhost:8080/api/admin/ingredients/$HARINA_ID/presentations" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Saco 1 kg",
    "purchaseUnit":"kg",
    "purchaseQuantity":1,
    "conversionToUseUnit":1000,
    "lastPurchasePriceUsd":1.20
  }' | jq -r .id)
echo "presentation=$PRES_ID"
```

## 7. Registrar compra (10 sacos @ $1.00) — el trigger SQL actualiza stock + avg cost

```bash
curl -s -X POST http://localhost:8080/api/admin/inventory/purchases \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"ingredientId\":\"$HARINA_ID\",
    \"presentationId\":\"$PRES_ID\",
    \"quantityPurchased\":10,
    \"unitPriceUsd\":1.00,
    \"supplier\":\"Mayorista La Bendición\",
    \"reference\":\"FAC-001\"
  }" | jq

# Verificar que el stock subió a 10000 g y avg cost = 0.001 USD/g
curl -s "http://localhost:8080/api/admin/ingredients/$HARINA_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq '.currentStockUseUnit, .avgCostPerUseUnitUsd'
# 10000 / 0.001
```

## 8. Actualizar umbrales (opcional, PATCH)

```bash
curl -s -X PATCH "http://localhost:8080/api/admin/ingredients/$HARINA_ID/thresholds" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reorderPointUseUnit":3000,"minimumStockUseUnit":500}'
# 204 No Content
```

## 9. Crear plato (arepa reina pepiada) — $5.00

```bash
AREPA_ID=$(curl -s -X POST http://localhost:8080/api/admin/recipes/dishes \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Arepa Reina Pepiada",
    "sellingPriceUsd":5.00,
    "prepTimeMinutes":15,
    "menuType":"fixed",
    "category":"plato fuerte",
    "description":"Arepa rellena con pollo y aguacate"
  }' | jq -r .id)
echo "arepa=$AREPA_ID"
```

## 10. Agregar componente: 100 g de harina por arepa

```bash
curl -s -X POST "http://localhost:8080/api/admin/recipes/$AREPA_ID/components/ingredient" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"ingredientId\":\"$HARINA_ID\",
    \"quantity\":100,
    \"notes\":\"Para la masa\",
    \"displayOrder\":1
  }" | jq
```

## 11. Verificar costo del plato — debe dar 0.10 USD (100 g × $0.001/g)

```bash
curl -s "http://localhost:8080/api/admin/recipes/$AREPA_ID/cost" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq
# totalCostUsd: 0.10, lines:[{ingredient:"Harina de maíz precocida", qty:100, lineCost:0.10}]
```

## 12. Activar el plato en el menú (poner precio + on-menu)

`CreateDish` ya marca `is_active=true, is_on_menu=true` por default (revisar Recipe aggregate). Si está en out-of-stock por alguna razón, se quita:

```bash
curl -s -X POST "http://localhost:8080/api/admin/recipes/$AREPA_ID/out-of-stock" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"outOfStock":false}'
# 204
```

## 13. Como cliente anónimo: listar menú + ver detalle

```bash
curl -s http://localhost:8080/api/client/menu | jq '.[0]'
# debe aparecer la arepa

curl -s "http://localhost:8080/api/client/menu/$AREPA_ID" | jq
# detalle público (sin precio interno de costo)
```

## 14. Crear orden guest (pickup)

```bash
ORDER_ID=$(curl -s -X POST http://localhost:8080/api/client/orders \
  -H "Content-Type: application/json" \
  -d "{
    \"guestFullName\":\"María Rodríguez\",
    \"guestPhone\":\"+58 414 1234567\",
    \"deliveryType\":\"pickup\",
    \"items\":[{\"dishId\":\"$AREPA_ID\",\"quantity\":2,\"itemNotes\":\"Sin cebolla\"}],
    \"customerNotes\":\"Para almuerzo\"
  }" | jq -r .id)
echo "order=$ORDER_ID"
```

## 15. Subir comprobante de pago (multipart)

```bash
# Crear un PNG mínimo válido (1x1 transparente)
printf '\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01\x08\x06\x00\x00\x00\x1f\x15\xc4\x89\x00\x00\x00\rIDATx\x9cc\xf8\x0f\x00\x00\x01\x01\x00\x05\xfe\x02\xfe\xa9\x35\x81\x84\x00\x00\x00\x00IEND\xaeB`\x82' > /tmp/proof.png

PROOF_URL=$(curl -s -X POST http://localhost:8080/api/uploads/payment-proofs \
  -F "file=@/tmp/proof.png;type=image/png" | jq -r .url)
echo "proof=$PROOF_URL"
# /uploads/payment-proofs/<guid>.png
```

## 16. Submit payment proof (Pago Móvil VES)

```bash
curl -s -X POST "http://localhost:8080/api/client/orders/$ORDER_ID/payment" \
  -H "Content-Type: application/json" \
  -d "{
    \"method\":\"pago_movil\",
    \"amountUsd\":10.00,
    \"paidCurrency\":\"VES\",
    \"amountPaidCurrency\":420.00,
    \"exchangeRateUsed\":42.00,
    \"referenceNumber\":\"123456789012\",
    \"proofImageUrl\":\"$PROOF_URL\",
    \"payerName\":\"María Rodríguez\",
    \"payerPhone\":\"04141234567\"
  }" | jq
# 201 con paymentId. Order pasa a payment_verifying.
```

## 17. Tracking del cliente (anónimo, por id)

```bash
curl -s "http://localhost:8080/api/client/orders/$ORDER_ID" | jq '.status, .totalUsd'
# "payment_verifying", 10.00
```

## 18. Admin: listar pagos pendientes

```bash
PAYMENT_ID=$(curl -s http://localhost:8080/api/admin/payments/pending \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq -r '.[0].id')
echo "payment=$PAYMENT_ID"
```

## 19. Admin: verificar el pago (acepta)

```bash
curl -s -X POST "http://localhost:8080/api/admin/payments/$PAYMENT_ID/verify" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
# 204. Order avanza a paid.
```

(Para probar reject:
`POST /api/admin/payments/$PAYMENT_ID/reject` con `{"reason":"comprobante ilegible"}` antes de verificar.)

## 20. Cocina (Admin tiene permiso de Cook)

```bash
# Listar órdenes activas en cocina
curl -s http://localhost:8080/api/kitchen/orders \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

# Cola enriquecida con procedure_markdown + prep time + priority
curl -s http://localhost:8080/api/kitchen/queue \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

# Avanzar admin la order a in_preparation primero (cashier flow)
curl -s -X POST "http://localhost:8080/api/admin/orders/$ORDER_ID/advance" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"target":"in_preparation"}'

# Obtener itemId del primer line item
ITEM_ID=$(curl -s "http://localhost:8080/api/admin/orders/$ORDER_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq -r '.items[0].id')

# Iniciar prep
curl -s -X POST "http://localhost:8080/api/kitchen/orders/$ORDER_ID/items/$ITEM_ID/start" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Marcar listo (cuando todos los items están listos, la orden pasa auto a "ready")
curl -s -X POST "http://localhost:8080/api/kitchen/orders/$ORDER_ID/items/$ITEM_ID/ready" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

## 21. Pickup → delivered

```bash
curl -s -X POST "http://localhost:8080/api/admin/orders/$ORDER_ID/advance" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"target":"delivered"}'

curl -s "http://localhost:8080/api/admin/orders/$ORDER_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq '.status'
# "delivered"
```

## 22. Recibo PDF (admin + cliente)

```bash
# Cliente — sin auth
curl -s "http://localhost:8080/api/client/orders/$ORDER_ID/receipt.pdf" \
  -o /tmp/recibo.pdf
file /tmp/recibo.pdf  # debe decir "PDF document"

# Admin
curl -s "http://localhost:8080/api/admin/orders/$ORDER_ID/receipt.pdf" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -o /tmp/recibo-admin.pdf
```

## 23. Emitir factura SENIAT (mock provider)

```bash
INVOICE=$(curl -s -X POST http://localhost:8080/api/admin/invoices \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"orderId\":\"$ORDER_ID\",
    \"customerRif\":\"V-12345678-9\",
    \"customerLegalName\":\"María Rodríguez\",
    \"customerAddress\":\"Caracas\"
  }")
INVOICE_ID=$(echo "$INVOICE" | jq -r .id)
echo "$INVOICE" | jq '.status, .fiscalNumber, .ivaUsd, .igtfUsd, .totalWithTaxUsd'
# status=issued · fiscal MOCK-00000001 · IVA=1.60 · IGTF=— (Pago Móvil no aplica) · total=11.60
```

Para probar IGTF, repetí el flujo con un payment de método `zelle` o `transfer_usd`.

## 24. Listar / get factura

```bash
curl -s "http://localhost:8080/api/admin/invoices?statusFilter=issued&days=30" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq '.[0]'

curl -s "http://localhost:8080/api/admin/invoices/$INVOICE_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq
```

## 25. PDF tributario — ahora trae datos fiscales

```bash
curl -s "http://localhost:8080/api/admin/orders/$ORDER_ID/receipt.pdf" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -o /tmp/factura.pdf
# El filename header debe decir factura-MOCK-00000001.pdf
```

## 26. Cancelar factura (FSM: issued → cancelled)

```bash
curl -s -X POST "http://localhost:8080/api/admin/invoices/$INVOICE_ID/cancel" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason":"Datos del cliente incorrectos"}'
# 204. PDF ahora muestra watermark ANULADA.
```

## 27. Reports + analytics

```bash
curl -s http://localhost:8080/api/admin/reports/dish-margin \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq '.[0]'
# margenBruto ~98% para la arepa ($5 - $0.10)

curl -s "http://localhost:8080/api/admin/reports/recipe-costs?includeSubRecipes=false" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

curl -s "http://localhost:8080/api/admin/reports/reorder-suggestions?priority=ok" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

curl -s "http://localhost:8080/api/admin/reports/sales-daily?days=7" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq
```

## 28. Forecast de compras

```bash
curl -s "http://localhost:8080/api/admin/purchasing/forecast?historicalDays=28&targetDays=7&growthFactor=1.0" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq
```

## 29. Mermas (waste)

```bash
curl -s -X POST http://localhost:8080/api/admin/inventory/waste \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"ingredientId\":\"$HARINA_ID\",
    \"quantityUseUnit\":50,
    \"reason\":\"spoiled\",
    \"notes\":\"Se humedeció el saco abierto\"
  }"
# 201
```

## 30. Webhook delivery (anon)

```bash
curl -s -X POST http://localhost:8080/api/webhooks/delivery/yummy \
  -H "Content-Type: application/json" \
  -d "{
    \"status\":\"delivered\",
    \"orderId\":\"$ORDER_ID\",
    \"courierName\":\"José Pérez\",
    \"courierPhone\":\"04141111111\",
    \"lat\":10.491,
    \"lng\":-66.902,
    \"eventAt\":\"2026-04-25T15:30:00-04:00\"
  }"
# 202 Accepted con eventId
```

(En este flujo la order ya está delivered, así que el evento solo registra tracking sin avanzar FSM.)

## 31. Registrar usuario cliente + reviews

```bash
CUSTOMER_TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"maria@example.com",
    "password":"Maria12345!",
    "fullName":"María Rodríguez",
    "phone":"+58 414 1234567"
  }' | jq -r .accessToken)

# Cambiar password
curl -s -X POST http://localhost:8080/api/auth/change-password \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"currentPassword":"Maria12345!","newPassword":"NuevaPass2026!"}'
# 204 (re-loguear con la nueva)
```

> **Nota:** la review requiere que el `OrderId` pertenezca al usuario logged in. La order de pasos anteriores fue creada como guest, así que para probar reviews tenés que crear una nueva orden **mientras estás autenticado** (el endpoint `/api/client/orders` POST acepta token y mete `customerId`). El smoke entonces sería: re-loguear cliente → crear orden auth → emparejar y avanzar a delivered → POST review.

## 32. Preferencias del cliente (sync onboarding)

```bash
# GET inicial — payload vacío
curl -s http://localhost:8080/api/client/me/preferences \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" | jq

# PUT — guardar
curl -s -X PUT http://localhost:8080/api/client/me/preferences \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "version":1,
    "defaultAddress":"Av. Francisco de Miranda, edif. La Floresta, apto 4-B",
    "dietary":["sin_lactosa"],
    "allergens":["mariscos"],
    "favoriteCategories":["plato fuerte","postre"],
    "wantsLoyaltyUpdates":true
  }'
# 204

# GET — debe devolver el blob + updatedAt
curl -s http://localhost:8080/api/client/me/preferences \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" | jq
```

## ✅ Checklist de cobertura

| # | Módulo | Endpoints validados |
|---|---|---|
| Auth | register, login, me, change-password | 4 |
| Client | menu list+detail, orders create+get+receipt+payment, reviews list, preferences GET+PUT, uploads | 11 |
| Admin Catalog | ingredients (5) + recipes (8) | 13 |
| Admin Operations | orders (3+receipt), payments (3), inventory (purchases+waste), invoices (4), purchasing forecast | 13 |
| Admin Reports | dish-margin, recipe-costs, reorder-suggestions, sales-daily | 4 |
| Kitchen | orders, queue, item start+ready | 4 |
| Webhooks | delivery POST | 1 |
| Health | /health, /health/db | 2 |
| **Total** | | **~52** (cobre los 40 funcionales + variantes) |

## Cuándo decir "el backend está sano"

Si los pasos **1-23 corren sin error** (admin login → forecast pipeline completo → factura emitida con números MOCK), el backend está completamente funcional. El resto es para validar features secundarias.

## Errores comunes

- **401 al hacer login del bootstrap admin:** falta `Bootstrap__Admin__Email` en las env vars del compose. Ver paso 0.
- **400 al crear order:** el plato está en `out-of-stock` o `is_active=false`. Verificar con `GET /api/admin/recipes/{id}`.
- **409 al emitir factura:** la order todavía no está en `delivered` o `ready`, o ya tiene factura.
- **DomainException "no puede registrar merma":** la cantidad excede el stock actual.
- **PDF en blanco:** falta `QuestPDF.Settings.License = LicenseType.Community` (ya está seteado en el `static ctor` del generator).
