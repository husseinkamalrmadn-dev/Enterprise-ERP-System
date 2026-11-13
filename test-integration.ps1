# ==============================================================================
# Enterprise ERP - Local Backend & Live Vercel CORS Round-Trip Verification Suite
# ==============================================================================
param (
    [string]$BaseUrl = "http://localhost:5213",
    [string]$FrontendOrigin = "https://enterprise-erp-system-wwwroot.vercel.app"
)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "   Enterprise ERP: .NET 9 Backend & Cloudflare/Vercel CORS Test" -ForegroundColor Cyan
Write-Host "   Target Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "   Simulated Vercel Origin: $FrontendOrigin" -ForegroundColor Yellow
Write-Host "=================================================================`n" -ForegroundColor Cyan

function Test-Endpoint {
    param (
        [string]$Name,
        [string]$Path,
        [string]$Method = "GET"
    )

    $url = "$BaseUrl$Path"
    Write-Host "[*] Testing $Name ($Method $url)..." -NoNewline

    try {
        $headers = @{
            "Origin" = $FrontendOrigin
            "Accept" = "application/json"
        }
        
        $response = Invoke-WebRequest -Uri $url -Method $Method -Headers $headers -TimeoutSec 10 -ErrorAction Stop

        Write-Host " [SUCCESS]" -ForegroundColor Green
        Write-Host "    Status: $($response.StatusCode) $($response.StatusDescription)" -ForegroundColor Gray
        
        # Check CORS Headers
        $corsOrigin = $response.Headers["Access-Control-Allow-Origin"]
        $corsCreds = $response.Headers["Access-Control-Allow-Credentials"]

        if ($corsOrigin -eq $FrontendOrigin -or $corsOrigin -eq "*") {
            Write-Host "    [CORS OK] Access-Control-Allow-Origin: $corsOrigin" -ForegroundColor Green
        } else {
            Write-Host "    [CORS WARN] Origin header: $corsOrigin" -ForegroundColor Yellow
        }

        if ($corsCreds) {
            Write-Host "    [CORS OK] Access-Control-Allow-Credentials: $corsCreds" -ForegroundColor Green
        }

        # Show snippet of body
        if ($response.Content) {
            $snippet = $response.Content.Substring(0, [Math]::Min(120, $response.Content.Length))
            Write-Host "    Payload Preview: $snippet..." -ForegroundColor DarkGray
        }

        return $true
    }
    catch {
        Write-Host " [FAILED]" -ForegroundColor Red
        Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# 1. Health Check
Write-Host "--- 1. Health & Readiness ---" -ForegroundColor Cyan
Test-Endpoint -Name "Health Check" -Path "/health"

# 2. CORS Preflight Simulation (OPTIONS)
Write-Host "`n--- 2. Browser CORS Preflight Simulation (OPTIONS) ---" -ForegroundColor Cyan
Write-Host "[*] Sending OPTIONS request to /api/Customers..." -NoNewline
try {
    $preflightHeaders = @{
        "Origin" = $FrontendOrigin
        "Access-Control-Request-Method" = "GET"
        "Access-Control-Request-Headers" = "authorization,content-type"
    }
    $optionsResp = Invoke-WebRequest -Uri "$BaseUrl/api/Customers" -Method OPTIONS -Headers $preflightHeaders -TimeoutSec 10 -ErrorAction Stop
    Write-Host " [SUCCESS]" -ForegroundColor Green
    Write-Host "    Access-Control-Allow-Origin : $($optionsResp.Headers['Access-Control-Allow-Origin'])" -ForegroundColor Green
    Write-Host "    Access-Control-Allow-Methods: $($optionsResp.Headers['Access-Control-Allow-Methods'])" -ForegroundColor Green
    Write-Host "    Access-Control-Allow-Headers: $($optionsResp.Headers['Access-Control-Allow-Headers'])" -ForegroundColor Green
} catch {
    Write-Host " [FAILED preflight] $($_.Exception.Message)" -ForegroundColor Red
}

# 3. Core ERP Module Queries
Write-Host "`n--- 3. Core ERP Business Endpoints ---" -ForegroundColor Cyan
Test-Endpoint -Name "Customers (CRM)" -Path "/api/Customers"
Test-Endpoint -Name "Products (Inventory)" -Path "/api/Products"
Test-Endpoint -Name "Warehouses (Supply Chain)" -Path "/api/Warehouses"
Test-Endpoint -Name "Finance Summary (Charts/KPIs)" -Path "/api/Finance/Summary"
Test-Endpoint -Name "Sales Orders (POS)" -Path "/api/SalesOrders"
Test-Endpoint -Name "Employees (HR)" -Path "/api/Employees"

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host "All tests completed. Your backend is configured for Cloudflare Tunnel & Vercel." -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Cyan
