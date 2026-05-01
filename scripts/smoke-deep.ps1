<#
.SYNOPSIS
    Smoke test profundo: recorre todo el flujo de negocio de HomeChef Pro
    end-to-end, sin manos.

.DESCRIPTION
    Asume que `smoke.ps1` ya corrio (admin@homechef.local + maria@example.com
    existen con password demo1234, BD seeded con 3 platos y 20 ingredientes).

    El flujo completo:
        1. Login admin + login cliente Maria
        2. Maria crea una orden (2 arepas + 1 pasticho, pickup)
        3. Maria sube comprobante de pago Pago Movil VES
        4. Admin lista pagos pendientes y verifica el de Maria
        5. Admin avanza la orden a in_preparation
        6. Cocina inicia prep de cada item -> mark ready
        7. Admin avanza a delivered
        8. Admin emite factura SENIAT (mock)
        9. Cliente y admin descargan recibo PDF
       10. Reportes: dish-margin y sales-daily reflejan la venta

    Cada paso loguea con [OK]/[XX] y al final imprime resumen.

.PARAMETER ApiBase
    Base URL del backend. Default http://localhost:8080.

.EXAMPLE
    pwsh ./scripts/smoke-deep.ps1
#>
[CmdletBinding()]
param(
    [string]$ApiBase = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"
$DemoPassword = "demo1234"

function Write-Step { param([string]$T) Write-Host ""; Write-Host "=== $T ===" -ForegroundColor Cyan }
function Write-Ok   { param([string]$M) Write-Host "  [OK] $M" -ForegroundColor Green }
function Write-Warn { param([string]$M) Write-Host "  [!!] $M" -ForegroundColor Yellow }
function Write-Fail { param([string]$M) Write-Host "  [XX] $M" -ForegroundColor Red; throw $M }
function HJson { param($Obj) $Obj | ConvertTo-Json -Compress -Depth 6 }

# Genera un PNG minimo valido (1x1 transparente) en bytes para el comprobante.
function New-DummyPng {
    $bytes = [byte[]](
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,
        0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,
        0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
        0x08,0x06,0x00,0x00,0x00,0x1F,0x15,0xC4,
        0x89,0x00,0x00,0x00,0x0D,0x49,0x44,0x41,
        0x54,0x78,0x9C,0x63,0xF8,0x0F,0x00,0x00,
        0x01,0x01,0x00,0x05,0xFE,0x02,0xFE,0xA9,
        0x35,0x81,0x84,0x00,0x00,0x00,0x00,0x49,
        0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
    )
    $tmp = [System.IO.Path]::GetTempFileName() + ".png"
    [System.IO.File]::WriteAllBytes($tmp, $bytes)
    return $tmp
}

# ---------------------------------------------------------------------
# 1. Auth
# ---------------------------------------------------------------------
Write-Step "1. Auth admin y Maria"

$adminLogin = HJson @{ email="admin@homechef.local"; password=$DemoPassword }
$mariaLogin = HJson @{ email="maria@example.com";    password=$DemoPassword }

try {
    $admin = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
        -ContentType "application/json" -Body $adminLogin
    $adminToken = $admin.accessToken
    Write-Ok "Admin login: $($admin.email) [$($admin.roles -join ',')]"
} catch { Write-Fail "Admin login fallo. Corriste smoke.ps1 antes?" }

try {
    $maria = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
        -ContentType "application/json" -Body $mariaLogin
    $mariaToken = $maria.accessToken
    Write-Ok "Maria login: $($maria.email) [$($maria.roles -join ',')]"
} catch { Write-Fail "Maria login fallo. Corriste smoke.ps1 antes?" }

$Hadmin = @{ Authorization = "Bearer $adminToken" }
$Hmaria = @{ Authorization = "Bearer $mariaToken" }

# ---------------------------------------------------------------------
# 2. Maria explora el menu publico
# ---------------------------------------------------------------------
Write-Step "2. Menu publico"
$menu = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/client/menu"
if ($menu.Count -lt 1) { Write-Fail "Menu vacio. Estan los seeds aplicados?" }
foreach ($d in $menu) {
    Write-Host ("       - {0,-30}  USD {1,6:N2}  ({2})" -f $d.name, $d.sellingPriceUsd, $d.id.Substring(0,8))
}
# Tomamos hasta 2 platos: el primero (qty 2) y el segundo si existe (qty 1).
$dish1 = $menu[0]
$dish2 = if ($menu.Count -ge 2) { $menu[1] } else { $null }

# ---------------------------------------------------------------------
# 3. Maria crea la orden
# ---------------------------------------------------------------------
Write-Step "3. Maria crea orden (pickup)"
$items = @( @{ dishId = $dish1.id; quantity = 2; itemNotes = "sin cebolla" } )
if ($dish2) { $items += @{ dishId = $dish2.id; quantity = 1 } }

$orderReq = HJson @{
    # El endpoint POST /api/client/orders siempre exige guestFullName/guestPhone,
    # incluso cuando el cliente esta autenticado (queda como guest order).
    guestFullName = "Maria Rodriguez"
    guestPhone    = "+58 414 1234567"
    deliveryType  = "pickup"
    items         = $items
    customerNotes = "smoke-deep"
}
$createResp = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/client/orders" `
    -Headers $Hmaria -ContentType "application/json" -Body $orderReq
$orderId = if ($createResp -is [string]) { $createResp } else { $createResp.id ?? $createResp }
if ([string]::IsNullOrWhiteSpace($orderId)) { Write-Fail "No vino orderId. Resp: $createResp" }
Write-Ok "Order creada: $orderId"

# Recuperamos detalles
$order = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/client/orders/$orderId"
Write-Ok "Number=$($order.orderNumber)  Status=$($order.status)  Total=USD $($order.totalUsd)"

# ---------------------------------------------------------------------
# 4. Maria sube comprobante + submit del pago
# ---------------------------------------------------------------------
Write-Step "4. Comprobante de pago + submit"

$proofPath = New-DummyPng
try {
    # PowerShell -Form envia application/octet-stream y el backend lo rechaza.
    # curl.exe tipa correctamente el part como image/png.
    $rawJson = & curl.exe -s -X POST "$ApiBase/api/uploads/payment-proofs" `
        -F "file=@${proofPath};type=image/png"
    if ($LASTEXITCODE -ne 0) { Write-Fail "curl fallo subiendo el comprobante." }
    $upload = $rawJson | ConvertFrom-Json
    $proofUrl = $upload.url
    Write-Ok "Comprobante subido: $proofUrl"
} finally { Remove-Item $proofPath -ErrorAction SilentlyContinue }

$payReq = HJson @{
    method                = "pago_movil"
    amountUsd             = $order.totalUsd
    paidCurrency          = "VES"
    amountPaidCurrency    = [math]::Round($order.totalUsd * 42, 2)
    exchangeRateUsed      = 42
    referenceNumber       = "999000111222"
    proofImageUrl         = $proofUrl
    payerName             = "Maria Rodriguez"
    payerPhone            = "04141234567"
}
$payment = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/client/orders/$orderId/payment" `
    -Headers $Hmaria -ContentType "application/json" -Body $payReq
Write-Ok "Pago enviado. paymentId=$($payment.id ?? $payment)"

$order = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/orders/$orderId" -Headers $Hadmin
Write-Ok "Order status -> $($order.status)"

# ---------------------------------------------------------------------
# 5. Admin verifica el pago
# ---------------------------------------------------------------------
Write-Step "5. Admin verifica el pago"
$pending = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/payments/pending" -Headers $Hadmin
$pendingForOrder = $pending | Where-Object { $_.orderId -eq $orderId } | Select-Object -First 1
if (-not $pendingForOrder) { Write-Fail "El pago de la orden no aparece en pending." }
Invoke-RestMethod -Method Post -Uri "$ApiBase/api/admin/payments/$($pendingForOrder.id)/verify" -Headers $Hadmin | Out-Null
Write-Ok "Pago $($pendingForOrder.id) verificado"

$order = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/orders/$orderId" -Headers $Hadmin
Write-Ok "Order status -> $($order.status)"

# ---------------------------------------------------------------------
# 6. Admin avanza a in_preparation y cocina prepara los items
# ---------------------------------------------------------------------
Write-Step "6. Cocina"
Invoke-RestMethod -Method Post -Uri "$ApiBase/api/admin/orders/$orderId/advance" `
    -Headers $Hadmin -ContentType "application/json" -Body (HJson @{ target = "in_preparation" }) | Out-Null
Write-Ok "Orden -> in_preparation"

$order = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/orders/$orderId" -Headers $Hadmin
foreach ($it in $order.items) {
    Invoke-RestMethod -Method Post -Uri "$ApiBase/api/kitchen/orders/$orderId/items/$($it.id)/start" -Headers $Hadmin | Out-Null
    Invoke-RestMethod -Method Post -Uri "$ApiBase/api/kitchen/orders/$orderId/items/$($it.id)/ready" -Headers $Hadmin | Out-Null
    Write-Ok "Item $($it.id.Substring(0,8)) ($($it.dishNameSnapshot)) prep -> ready"
}

$order = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/orders/$orderId" -Headers $Hadmin
Write-Ok "Order status -> $($order.status)"

# ---------------------------------------------------------------------
# 7. Admin marca delivered (es pickup, asi que es directo)
# ---------------------------------------------------------------------
Write-Step "7. Entrega"
Invoke-RestMethod -Method Post -Uri "$ApiBase/api/admin/orders/$orderId/advance" `
    -Headers $Hadmin -ContentType "application/json" -Body (HJson @{ target = "delivered" }) | Out-Null
$order = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/orders/$orderId" -Headers $Hadmin
Write-Ok "Order status -> $($order.status)"

# ---------------------------------------------------------------------
# 8. Admin emite factura SENIAT (mock)
# ---------------------------------------------------------------------
Write-Step "8. Factura SENIAT mock"
$invReq = HJson @{
    orderId           = $orderId
    customerRif       = "V-12345678-9"
    customerLegalName = "Maria Rodriguez"
    customerAddress   = "Caracas"
}
$invoice = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/admin/invoices" `
    -Headers $Hadmin -ContentType "application/json" -Body $invReq
Write-Ok "Factura emitida: $($invoice.fiscalNumber)  status=$($invoice.status)"
Write-Ok "IVA=USD $($invoice.ivaUsd)  IGTF=USD $($invoice.igtfUsd)  Total=USD $($invoice.totalWithTaxUsd)"

# ---------------------------------------------------------------------
# 9. Recibo PDF (cliente anonimo + admin)
# ---------------------------------------------------------------------
Write-Step "9. Recibo PDF"
$tmpClient = [System.IO.Path]::GetTempFileName() + ".pdf"
$tmpAdmin  = [System.IO.Path]::GetTempFileName() + ".pdf"
try {
    Invoke-WebRequest -Uri "$ApiBase/api/client/orders/$orderId/receipt.pdf" -OutFile $tmpClient | Out-Null
    Invoke-WebRequest -Uri "$ApiBase/api/admin/orders/$orderId/receipt.pdf" -Headers $Hadmin -OutFile $tmpAdmin | Out-Null
    $sizeC = (Get-Item $tmpClient).Length
    $sizeA = (Get-Item $tmpAdmin).Length
    Write-Ok "Cliente PDF: $sizeC bytes"
    Write-Ok "Admin PDF:   $sizeA bytes"
} finally {
    Remove-Item $tmpClient,$tmpAdmin -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------------
# 10. Reportes refrejan la venta
# ---------------------------------------------------------------------
Write-Step "10. Reportes"
$margin = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/reports/dish-margin" -Headers $Hadmin
$top = $margin | Sort-Object -Property grossMarginPct -Descending | Select-Object -First 3
foreach ($r in $top) {
    Write-Host ("       - {0,-30}  margen {1,5:N1}%  costo USD {2:N2}" -f $r.name, $r.grossMarginPct, $r.totalCostUsd)
}

$daily = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/reports/sales-daily?days=7" -Headers $Hadmin
$today = $daily | Sort-Object -Property saleDate -Descending | Select-Object -First 1
if ($today) {
    # saleDate viene como string ISO o DateTime segun el deserializer; tomamos
    # los primeros 10 chars (yyyy-MM-dd) sin asumir tipo.
    $dateStr = ([string]$today.saleDate).Substring(0, [Math]::Min(10, ([string]$today.saleDate).Length))
    Write-Ok "Hoy ($dateStr): $($today.ordersCount) ordenes  USD $($today.revenueUsd)  ganancia USD $($today.grossProfitUsd)"
}

# ---------------------------------------------------------------------
# Resumen final
# ---------------------------------------------------------------------
Write-Step "Resumen"
Write-Host "  Order             : $($order.orderNumber)  ($orderId)" -ForegroundColor Gray
Write-Host "  Items             : $($order.items.Count)"
Write-Host "  Total final       : USD $($order.totalUsd)"
Write-Host "  Status            : $($order.status)"
Write-Host "  Factura           : $($invoice.fiscalNumber) ($($invoice.status))"
Write-Host ""
Write-Host "Smoke profundo OK. La BD ya tiene una venta entregada con factura."
Write-Host "Refresca admin_web (:8090) -> Resumen / Analitica para ver los KPI vivos."
Write-Host ""
