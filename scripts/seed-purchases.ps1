<#
.SYNOPSIS
    Seedeo de compras: registra una compra por cada ingrediente activo,
    usando la primera presentacion disponible. El trigger SQL actualiza
    stock y avg_cost_per_use_unit en cascada, asi los reportes muestran
    margenes reales y el catalogo no esta en cero.

.DESCRIPTION
    Idempotente: si ya hay compras registradas, igualmente agrega otras
    (la BD permite multiples compras del mismo insumo).

    Para resetear y probar limpio: docker compose down -v y arrancar fresh.

.PARAMETER ApiBase
    Default http://localhost:8080

.PARAMETER Quantity
    Cantidad de presentaciones a comprar (default 5).

.EXAMPLE
    pwsh ./scripts/seed-purchases.ps1
    pwsh ./scripts/seed-purchases.ps1 -Quantity 10
#>
[CmdletBinding()]
param(
    [string]$ApiBase = "http://localhost:8080",
    [int]$Quantity = 5
)

$ErrorActionPreference = "Stop"

function Write-Step { param([string]$T) Write-Host ""; Write-Host "=== $T ===" -ForegroundColor Cyan }
function Write-Ok   { param([string]$M) Write-Host "  [OK] $M" -ForegroundColor Green }
function Write-Warn { param([string]$M) Write-Host "  [!!] $M" -ForegroundColor Yellow }
function Write-Fail { param([string]$M) Write-Host "  [XX] $M" -ForegroundColor Red; throw $M }

Write-Step "Login admin"
$body = @{ email = "admin@homechef.local"; password = "demo1234" } | ConvertTo-Json -Compress
try {
    $auth = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
        -ContentType "application/json" -Body $body
    $token = $auth.accessToken
    Write-Ok "Admin OK"
} catch { Write-Fail "Login admin fallo. Backend up? smoke.ps1 corrio?" }
$H = @{ Authorization = "Bearer $token" }

Write-Step "Listando ingredientes activos"
$list = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/ingredients?onlyActive=true" -Headers $H
Write-Ok "$($list.Count) ingredientes activos"

$bought = 0; $skipped = 0
foreach ($ing in $list) {
    # Detalle trae las presentaciones
    $det = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/ingredients/$($ing.id)" -Headers $H
    $pres = ($det.presentations | Where-Object { $_.isActive }) | Select-Object -First 1
    if (-not $pres) {
        Write-Warn "$($ing.name): sin presentacion activa, salteo"
        $skipped++
        continue
    }

    $price = if ($pres.lastPurchasePriceUsd) { [double]$pres.lastPurchasePriceUsd } else { 1.00 }

    $purchase = @{
        ingredientId      = $ing.id
        presentationId    = $pres.id
        quantityPurchased = $Quantity
        unitPriceUsd      = $price
        supplier          = "Mayorista La Bendicion (seed)"
        reference         = "SEED-$($ing.id.Substring(0,8))"
    } | ConvertTo-Json -Compress

    try {
        Invoke-RestMethod -Method Post -Uri "$ApiBase/api/admin/inventory/purchases" `
            -Headers $H -ContentType "application/json" -Body $purchase | Out-Null
        $totalUseUnits = $Quantity * [double]$pres.conversionToUseUnit
        $avgCost = $price / [double]$pres.conversionToUseUnit
        Write-Ok ("{0,-25} {1,3} x {2,-12} -> stock +{3,7:N0} {4}  avg \$/{5} = {6:N6}" -f `
            $ing.name, $Quantity, $pres.name, $totalUseUnits, $det.useUnit, $det.useUnit, $avgCost)
        $bought++
    } catch {
        Write-Warn "$($ing.name) fallo: $_"
        $skipped++
    }
}

Write-Step "Resumen"
Write-Host "  Compras OK : $bought"
Write-Host "  Salteados  : $skipped"
Write-Host ""
Write-Host "Refresca admin_web -> Inventario, deberian aparecer stock > 0 y avg cost > 0."
Write-Host "Volve a correr smoke-deep.ps1 para ver margenes reales en dish-margin (~75-90%)."
