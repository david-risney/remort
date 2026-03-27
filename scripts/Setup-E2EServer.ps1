<#
.SYNOPSIS
    Helps set up a local RDP server for Remort E2E tests.

.DESCRIPTION
    Checks available options for running a local RDP endpoint and guides setup.
    Three methods are supported:
      1. Windows loopback RDP (localhost:3389) — requires admin to enable
      2. FreeRDP shadow server — user-mode, no admin required
      3. Manual: set REMORT_E2E_RDP_HOST=hostname:port

.EXAMPLE
    .\Setup-E2EServer.ps1
    .\Setup-E2EServer.ps1 -EnableWindowsRdp
#>
[CmdletBinding()]
param(
    [switch]$EnableWindowsRdp
)

$ErrorActionPreference = 'Stop'

Write-Host "`n=== Remort E2E Test Server Setup ===" -ForegroundColor Cyan

# --- Check 1: Windows loopback RDP ---
Write-Host "`n[1] Windows Loopback RDP (localhost:3389)" -ForegroundColor Yellow
try {
    $ts = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server' -Name 'fDenyTSConnections' -ErrorAction Stop
    if ($ts.fDenyTSConnections -eq 0) {
        Write-Host "  ✅ RDP is enabled. Checking service..." -ForegroundColor Green
        $svc = Get-Service -Name TermService -ErrorAction SilentlyContinue
        if ($svc -and $svc.Status -eq 'Running') {
            Write-Host "  ✅ TermService is running. Loopback RDP is ready!" -ForegroundColor Green
            Write-Host "  Run tests with: dotnet test --filter Category=E2E" -ForegroundColor White
            exit 0
        } else {
            Write-Host "  ⚠️  RDP is enabled but TermService is not running." -ForegroundColor Yellow
            Write-Host "  Start it with: Start-Service TermService (requires admin)" -ForegroundColor White
        }
    } else {
        Write-Host "  ❌ RDP is disabled (fDenyTSConnections=1)." -ForegroundColor Red
        if ($EnableWindowsRdp) {
            Write-Host "  Enabling RDP..." -ForegroundColor Yellow
            Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server' -Name 'fDenyTSConnections' -Value 0
            Start-Service TermService
            Write-Host "  ✅ RDP enabled and service started." -ForegroundColor Green
            exit 0
        } else {
            Write-Host "  To enable: run this script as admin with -EnableWindowsRdp" -ForegroundColor White
        }
    }
} catch {
    Write-Host "  ❌ Cannot read RDP registry (run as admin): $_" -ForegroundColor Red
}

# --- Check 2: FreeRDP shadow server ---
Write-Host "`n[2] FreeRDP Shadow Server" -ForegroundColor Yellow
$freerdpNames = @('wfreerdp-shadow-cli.exe', 'wfreerdp-shadow.exe')
$found = $null
foreach ($name in $freerdpNames) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($cmd) { $found = $cmd.Source; break }
}

if ($found) {
    Write-Host "  ✅ Found: $found" -ForegroundColor Green
    Write-Host "  Start with: $found /port:13389" -ForegroundColor White
    Write-Host "  Then set: `$env:REMORT_E2E_RDP_HOST = '127.0.0.1:13389'" -ForegroundColor White
} else {
    Write-Host "  ❌ FreeRDP not found in PATH." -ForegroundColor Red
    Write-Host "  FreeRDP does NOT provide prebuilt Windows binaries." -ForegroundColor Yellow
    Write-Host "  Building from source requires CMake + Visual Studio." -ForegroundColor Yellow
    Write-Host "  See: https://github.com/FreeRDP/FreeRDP/wiki/Compilation" -ForegroundColor White
    Write-Host "  For most users, option 1 or 3 is simpler." -ForegroundColor White
}

# --- Check 3: Manual override ---
Write-Host "`n[3] Remote Host (easiest if you have a machine to RDP to)" -ForegroundColor Yellow
$envHost = $env:REMORT_E2E_RDP_HOST
if ($envHost) {
    Write-Host "  REMORT_E2E_RDP_HOST is set to: $envHost" -ForegroundColor Green
} else {
    Write-Host "  Point to any machine you can RDP to — no local setup needed." -ForegroundColor White
    Write-Host "  Example: `$env:REMORT_E2E_RDP_HOST = 'davris-4'" -ForegroundColor White
    Write-Host "  Example: `$env:REMORT_E2E_RDP_HOST = 'myserver.contoso.com:3389'" -ForegroundColor White
    Write-Host "  Note: the full E2E connect test will prompt for credentials." -ForegroundColor Gray
}

Write-Host "`n--- Running E2E tests ---" -ForegroundColor Cyan
Write-Host "  dotnet test --filter Category=E2E" -ForegroundColor White
Write-Host "  Tests skip automatically if no RDP server is reachable.`n" -ForegroundColor Gray
