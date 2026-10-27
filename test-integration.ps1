# ==============================================================================
# Enterprise ERP - Live Integration & CORS Verification Suite
# ==============================================================================
param (
    [string]$BaseUrl = "http://localhost:5213",
    [string]$FrontendOrigin = "https://enterprise-erp-system-wwwroot.vercel.app"
)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Enterprise ERP: Cloud & CORS Validation Engine" -ForegroundColor Cyan
Write-Host "Target Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Simulated Origin: $FrontendOrigin" -ForegroundColor Yellow
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
            Write-Host "    [CORS NOTE] Origin header: $corsOrigin" -ForegroundColor Yellow
        }

        if ($corsCreds) {
            Write-Host "    [CORS OK] Access-Control-Allow-Credentials: $corsCreds" -ForegroundColor Green
        }

        # Show snippet of body
        if ($response.Content) {
            $snippet = $response.Content.Substring(0, [Math]::Min(150, $response.Content.Length))
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
Test-Endpoint -Name "Health Check" -Path "/health"

# 2. CORS Preflight Simulation (OPTIONS)
Write-Host "`n[*] Simulating Browser CORS Preflight (OPTIONS /api/Customers)..." -NoNewline
try {
    $preflightHeaders = @{
        "Origin" = $FrontendOrigin
        "Access-Control-Request-Method" = "GET"
        "Access-Control-Request-Headers" = "authorization,content-type"
    }
    $optionsResp = Invoke-WebRequest -Uri "$BaseUrl/api/Customers" -Method OPTIONS -Headers $preflightHeaders -TimeoutSec 10 -ErrorAction Stop
    Write-Host " [SUCCESS]" -ForegroundColor Green
    Write-Host "    Allowed Origin: $($optionsResp.Headers['Access-Control-Allow-Origin'])" -ForegroundColor Green
    Write-Host "    Allowed Methods: $($optionsResp.Headers['Access-Control-Allow-Methods'])" -ForegroundColor Green
    Write-Host "    Allowed Headers: $($optionsResp.Headers['Access-Control-Allow-Headers'])" -ForegroundColor Green
} catch {
    Write-Host " [FAILED preflight] $($_.Exception.Message)" -ForegroundColor Yellow
}

# 3. Customers API
Test-Endpoint -Name "Customers Query" -Path "/api/Customers"

# 4. Finance Summary API
Test-Endpoint -Name "Finance Summary" -Path "/api/Finance/Summary"

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host "Validation complete." -ForegroundColor Cyan
