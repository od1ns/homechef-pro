<#
.SYNOPSIS
    Smoke test multi-usuario para HomeChef Pro.

.DESCRIPTION
    Levanta dos identidades distintas (Admin + Customer) y valida el flujo
    end-to-end mas chico que demuestra que el backend funciona:
        1. /health y /health/db
        2. Login del admin bootstrap
        3. Registro del cliente Maria (o login si ya existe)
        4. Listado de menu publico (anonimo y autenticado)

    Todos los usuarios de demo usan la password "demo1234".

.PARAMETER ApiBase
    Base URL de la API. Default: http://localhost:8080

.EXAMPLE
    pwsh ./scripts/smoke.ps1
    pwsh ./scripts/smoke.ps1 -ApiBase http://hcp.example.com
#>
[CmdletBinding()]
param(
    [string]$ApiBase         = "http://localhost:8080",
    [string]$AdminEmail      = "admin@homechef.local",
    [string]$AdminPassword   = "demo1234",
    [string]$ClientEmail     = "maria@example.com",
    [string]$ClientPassword  = "demo1234"
)

$ErrorActionPreference = "Stop"
# Compat: variable original. Cada bloque de login reasigna a la cuenta correspondiente.
$DemoPassword = $AdminPassword

function Write-Step {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

function Write-Ok    { param([string]$Msg) Write-Host "  [OK] $Msg" -ForegroundColor Green }
function Write-Warn  { param([string]$Msg) Write-Host "  [!!] $Msg" -ForegroundColor Yellow }
function Write-Fail  { param([string]$Msg) Write-Host "  [XX] $Msg" -ForegroundColor Red; throw $Msg }

# ---------------------------------------------------------------------
# 1. Health checks
# ---------------------------------------------------------------------
Write-Step "1. Health checks"

try {
    $h = Invoke-RestMethod -Method Get -Uri "$ApiBase/health" -TimeoutSec 5
    Write-Ok "GET /health -> status=$($h.status) service=$($h.service)"
} catch {
    Write-Fail "El API no responde en $ApiBase. Levantaste docker compose up -d?"
}

try {
    $hdb = Invoke-RestMethod -Method Get -Uri "$ApiBase/health/db" -TimeoutSec 5
    Write-Ok "GET /health/db -> ingredients=$($hdb.ingredients) recipes=$($hdb.recipes)"
} catch {
    Write-Warn "GET /health/db fallo. Postgres puede no haber arrancado todavia."
}

# ---------------------------------------------------------------------
# 2. Login del admin bootstrap
# ---------------------------------------------------------------------
Write-Step "2. Login admin (admin@homechef.local / $DemoPassword)"

$adminLogin = @{
    email    = "admin@homechef.local"
    password = $DemoPassword
} | ConvertTo-Json -Compress

try {
    $admin = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
        -ContentType "application/json" -Body $adminLogin
    $adminToken = $admin.accessToken
    Write-Ok "Login exitoso. userId=$($admin.userId) roles=$($admin.roles -join ',')"
    Write-Ok "Token: $($adminToken.Substring(0, 32))..."
} catch {
    Write-Fail "Login admin fallo. Bootstrap_Admin_Password en .env coincide con demo1234?"
}

$adminHeaders = @{ Authorization = "Bearer $adminToken" }

# /api/auth/me confirma que el JWT es valido y trae los roles
$me = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/auth/me" -Headers $adminHeaders
Write-Ok "GET /api/auth/me -> $($me.email) [$($me.roles -join ',')]"

# ---------------------------------------------------------------------
# 3. Registro o login del cliente Maria
# ---------------------------------------------------------------------
$DemoPassword = $ClientPassword
Write-Step "3. Cliente Maria (maria@example.com / $DemoPassword)"

$mariaCreds = @{
    email    = "maria@example.com"
    password = $DemoPassword
    fullName = "Maria Rodriguez"
    phone    = "+58 414 1234567"
} | ConvertTo-Json -Compress

$mariaToken = $null
try {
    $maria = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/register" `
        -ContentType "application/json" -Body $mariaCreds
    $mariaToken = $maria.accessToken
    Write-Ok "Cliente registrado. userId=$($maria.userId)"
} catch {
    # Probablemente ya existe -> intentamos login
    Write-Warn "Registro fallo (probablemente ya existe). Intentando login..."
    $mariaLogin = @{
        email    = "maria@example.com"
        password = $DemoPassword
    } | ConvertTo-Json -Compress
    try {
        $maria = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
            -ContentType "application/json" -Body $mariaLogin
        $mariaToken = $maria.accessToken
        Write-Ok "Login Maria exitoso. roles=$($maria.roles -join ',')"
    } catch {
        Write-Fail "No se pudo registrar ni loguear a Maria."
    }
}

$mariaHeaders = @{ Authorization = "Bearer $mariaToken" }
$meMaria = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/auth/me" -Headers $mariaHeaders
Write-Ok "GET /api/auth/me -> $($meMaria.email) [$($meMaria.roles -join ',')]"

# ---------------------------------------------------------------------
# 4. Catalogo publico (anonimo) y privado (con token)
# ---------------------------------------------------------------------
Write-Step "4. Catalogo de menu"

$menuAnon = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/client/menu"
Write-Ok "Menu anonimo: $($menuAnon.Count) plato(s)"
foreach ($d in $menuAnon | Select-Object -First 3) {
    Write-Host ("       - {0,-30}  USD {1,6:N2}" -f $d.name, $d.sellingPriceUsd)
}

# Lista de admin (incluye sub-recetas y campos privados)
$recipesAdmin = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/recipes" -Headers $adminHeaders
Write-Ok "Catalogo admin: $($recipesAdmin.Count) recetas (platos + sub-recetas)"

# Lista de ingredientes (solo admin)
$ingredients = Invoke-RestMethod -Method Get -Uri "$ApiBase/api/admin/ingredients?onlyActive=true" -Headers $adminHeaders
Write-Ok "Insumos activos: $($ingredients.Count)"

# ---------------------------------------------------------------------
# 5. Resumen
# ---------------------------------------------------------------------
Write-Step "Resumen"
Write-Host "  Admin    : admin@homechef.local   / $DemoPassword   roles=Admin"
Write-Host "  Client   : maria@example.com      / $DemoPassword   roles=Client"
Write-Host ""
Write-Host "Backend OK. Para probar el flujo completo (orden + pago + cocina + factura)"
Write-Host "ver docs/SMOKE.md, que cubre los 30+ endpoints con curl."
Write-Host ""
