<#
.SYNOPSIS
    Automated MonsterASP.NET Live Production & Vercel Verification Suite
#>
param(
    [string]$BackendUrl = "https://enterprise-erp-api.runasp.net",
    [string]$VercelOrigin = "https://enterprise-erp-system-wwwroot.vercel.app"
)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "   Enterprise ERP: Production Health & CORS Verification Engine" -ForegroundColor Cyan
Write-Host "   Backend URL  : $BackendUrl" -ForegroundColor Yellow
Write-Host "   Vercel Origin: $VercelOrigin" -ForegroundColor Yellow
Write-Host "=================================================================`n" -ForegroundColor Cyan

function Test-LiveEndpoint {
    param(
        [string]$Name,
        [string]$Path,
        [string]$Method = "GET"
    )

    $url = "$BackendUrl$Path"
    Write-Host "[*] Testing $Name ($Method $url)..." -NoNewline

    try {
        $headers = @{
            "Origin" = $VercelOrigin
            "Accept" = "application/json"
        }
        $resp = Invoke-WebRequest -Uri $url -Method $Method -Headers $headers -TimeoutSec 15 -UseBasicParsing
        Write-Host " [SUCCESS 200 OK]" -ForegroundColor Green
        
        $cors = $resp.Headers["Access-Control-Allow-Origin"]
        if ($cors) {
            Write-Host "    CORS Origin Allowed: $cors" -ForegroundColor Green
        }
        
        if ($resp.Content -and $resp.Content.Length -gt 0) {
            $snippet = $resp.Content.Substring(0, [Math]::Min(120, $resp.Content.Length)).Replace("`r","").Replace("`n","")
            Write-Host "    Preview: $snippet..." -ForegroundColor DarkGray
        }
        return $true
    }
    catch {
        Write-Host " [FAIL]" -ForegroundColor Red
        Write-Host "    Detail: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# 1. Health Probe
Write-Host "--- 1. Liveness & Database Check ---" -ForegroundColor Cyan
Test-LiveEndpoint -Name "Health Probe" -Path "/health"

# 2. Swagger Check
Write-Host "`n--- 2. Swagger OpenAPI Documentation ---" -ForegroundColor Cyan
Test-LiveEndpoint -Name "Swagger UI HTML" -Path "/swagger/index.html"
Test-LiveEndpoint -Name "OpenAPI JSON Spec" -Path "/swagger/v1/swagger.json"

# 3. CORS Preflight Simulation
Write-Host "`n--- 3. Browser CORS Preflight Simulation (OPTIONS) ---" -ForegroundColor Cyan
Write-Host "[*] Sending OPTIONS preflight to /api/Customers..." -NoNewline
try {
    $preflightHeaders = @{
        "Origin" = $VercelOrigin
        "Access-Control-Request-Method" = "GET"
        "Access-Control-Request-Headers" = "authorization,content-type"
    }
    $optionsResp = Invoke-WebRequest -Uri "$BackendUrl/api/Customers" -Method OPTIONS -Headers $preflightHeaders -TimeoutSec 15 -UseBasicParsing
    Write-Host " [SUCCESS]" -ForegroundColor Green
    Write-Host "    Allow-Origin : $($optionsResp.Headers['Access-Control-Allow-Origin'])" -ForegroundColor Green
    Write-Host "    Allow-Methods: $($optionsResp.Headers['Access-Control-Allow-Methods'])" -ForegroundColor Green
    Write-Host "    Allow-Headers: $($optionsResp.Headers['Access-Control-Allow-Headers'])" -ForegroundColor Green
} catch {
    Write-Host " [FAILED] $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Core Business Endpoints
Write-Host "`n--- 4. Core ERP Data Endpoints ---" -ForegroundColor Cyan
Test-LiveEndpoint -Name "Customers (CRM)" -Path "/api/Customers"
Test-LiveEndpoint -Name "Finance Summary (KPIs/Charts)" -Path "/api/Finance/Summary"
Test-LiveEndpoint -Name "Products (Inventory)" -Path "/api/Products"

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host "Verification routine finished." -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
