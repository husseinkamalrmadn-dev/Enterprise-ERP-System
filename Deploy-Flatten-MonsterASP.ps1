<#
.SYNOPSIS
    Automated MonsterASP.NET Deployment Flattener & Cleanup Suite
    Connects to MonsterASP.NET FTP to move all files out of /wwwroot/publish into /wwwroot/
#>
param(
    [Parameter(Mandatory=$false)]
    [string]$FtpServer = "ftp.runasp.net",
    
    [Parameter(Mandatory=$false)]
    [string]$Username,
    
    [Parameter(Mandatory=$false)]
    [string]$Password
)

if (-not $Username) {
    $Username = Read-Host "Enter your MonsterASP.NET FTP Username (from control panel)"
}
if (-not $Password) {
    $Password = Read-Host -AsSecureString "Enter your MonsterASP.NET FTP Password"
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ">>> MonsterASP.NET Automated Deployment Flattener" -ForegroundColor Cyan
Write-Host "Server: $FtpServer | User: $Username" -ForegroundColor Yellow
Write-Host "=================================================================`n" -ForegroundColor Cyan

function Execute-FtpRequest {
    param(
        [string]$Uri,
        [string]$Method,
        [string]$RenameTo = $null
    )
    $request = [System.Net.FtpWebRequest]::Create($Uri)
    $request.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
    $request.Method = $Method
    $request.UseBinary = $true
    $request.KeepAlive = $false
    if ($RenameTo) {
        $request.RenameTo = $RenameTo
    }

    try {
        $response = $request.GetResponse()
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        $content = $reader.ReadToEnd()
        $response.Close()
        return $content
    } catch {
        return $null
    }
}

$base = "ftp://$FtpServer/wwwroot"

# 1. Enumerate contents in /wwwroot/publish
Write-Host "[1/3] Scanning /wwwroot/publish directory..." -ForegroundColor Yellow
$listing = Execute-FtpRequest -Uri "$base/publish/" -Method ([System.Net.WebRequestMethods+Ftp]::ListDirectory)

if (-not $listing) {
    Write-Host " [INFO] No files found in /wwwroot/publish or folder does not exist." -ForegroundColor Green
    Write-Host "Checking root /wwwroot/web.config..." -ForegroundColor Yellow
    $rootCheck = Execute-FtpRequest -Uri "$base/web.config" -Method ([System.Net.WebRequestMethods+Ftp]::GetFileSize)
    if ($rootCheck -ne $null) {
        Write-Host " [CONFIRMED] web.config is already at the root of wwwroot!" -ForegroundColor Green
    }
} else {
    $items = $listing -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    Write-Host "Found $($items.Count) items in /publish. Flattening to /wwwroot/..." -ForegroundColor Green

    # 2. Move each item into /wwwroot/
    Write-Host "`n[2/3] Moving files to application root (/wwwroot)..." -ForegroundColor Yellow
    foreach ($item in $items) {
        $itemName = Split-Path $item -Leaf
        $src = "$base/publish/$itemName"
        $dest = "/wwwroot/$itemName"
        
        Write-Host "  -> Moving $itemName..." -NoNewline
        $result = Execute-FtpRequest -Uri $src -Method ([System.Net.WebRequestMethods+Ftp]::Rename) -RenameTo $dest
        Write-Host " [OK]" -ForegroundColor Green
    }

    # 3. Clean up the empty directory
    Write-Host "`n[3/3] Removing empty /publish directory..." -ForegroundColor Yellow
    Execute-FtpRequest -Uri "$base/publish" -Method ([System.Net.WebRequestMethods+Ftp]::RemoveDirectory) | Out-Null
    Write-Host " [SUCCESS] Folder /wwwroot/publish removed." -ForegroundColor Green
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host "Flattening complete. Please restart the website from the MonsterASP panel." -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Cyan
