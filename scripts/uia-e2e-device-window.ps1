#!/usr/bin/env pwsh
# E2E tests: Device window pages — General, Display, Redirections, nav/RDP toggle
# Prereq: app must be built (dotnet build Remort.sln)

$ErrorActionPreference = 'Continue'
. "$PSScriptRoot/uia-helpers.ps1"

Write-Host "`n=== E2E: Device Window Pages ===" -ForegroundColor Cyan

$dataDir = New-TempDataDir
Write-Host "Using temp data dir: $dataDir"

$app = Start-RemortApp -DataDir $dataDir
$window = Find-Window "Remort"
if (-not $window) { Write-Host "ABORT: Main window not found" -ForegroundColor Red; exit 1 }

# Add a test device
Write-Host "`nSetup: Adding test device..."
$added = Add-TestDevice $window "DWTest" "dwtest.local"
if (-not $added) { Write-Host "ABORT: Could not add device" -ForegroundColor Red; Stop-RemortApp; exit 1 }

# Open device window
$card = Find-ByName $window "DWTest"
if (-not $card) { Write-Host "ABORT: Card 'DWTest' not found" -ForegroundColor Red; Stop-RemortApp; exit 1 }
Write-Host "Found card, clicking..."
Click-Element $card | Out-Null
Start-Sleep -Seconds 4
$dw = Find-Window "DWTest" 10000
if (-not $dw) { Write-Host "ABORT: Device window not found" -ForegroundColor Red; Stop-RemortApp; exit 1 }

# --- Test DW1: Connection page is default ---
Write-Host "`n--- DW1: Connection page is default ---"
$connectBtn = Find-ByName $dw "Connect"
Write-TestResult "Connection page has Connect button" ($null -ne $connectBtn) "No Connect button"

# --- Test DW2: Navigate to General page ---
Write-Host "`n--- DW2: Navigate to General ---"
$generalNav = Find-ByName $dw "General"
if ($generalNav) {
    Click-Element $generalNav | Out-Null
    Start-Sleep -Seconds 1
    $nameField = Find-ByName $dw "Name"
    $hostnameField = Find-ByName $dw "Hostname"
    $autohide = Find-ByName $dw "Autohide title bar"
    $favCheckbox = Find-ByName $dw "Favorite"
    Write-TestResult "Name label visible" ($null -ne $nameField) "No 'Name' text"
    Write-TestResult "Hostname label visible" ($null -ne $hostnameField) "No 'Hostname' text"
    Write-TestResult "Autohide checkbox visible" ($null -ne $autohide) "No 'Autohide title bar' checkbox"
    Write-TestResult "Favorite checkbox visible" ($null -ne $favCheckbox) "No 'Favorite' checkbox"
} else {
    Write-TestResult "General nav item found" $false "Not in tree"
}

# --- Test DW3: Navigate to Display page ---
Write-Host "`n--- DW3: Navigate to Display ---"
$displayNav = Find-ByName $dw "Display"
if ($displayNav) {
    Click-Element $displayNav | Out-Null
    Start-Sleep -Seconds 1
    $pinVD = Find-ByName $dw "Pin to virtual desktop"
    $fitSession = Find-ByName $dw "Fit session to window"
    $allMonitors = Find-ByName $dw "Use all monitors when fullscreen"
    Write-TestResult "Pin to VD checkbox" ($null -ne $pinVD) "Not found"
    Write-TestResult "Fit session checkbox" ($null -ne $fitSession) "Not found"
    Write-TestResult "All monitors checkbox" ($null -ne $allMonitors) "Not found"
} else {
    Write-TestResult "Display nav item found" $false "Not in tree"
}

# --- Test DW4: Navigate to Redirections page ---
Write-Host "`n--- DW4: Navigate to Redirections ---"
$redirNav = Find-ByName $dw "Redirections"
if ($redirNav) {
    Click-Element $redirNav | Out-Null
    Start-Sleep -Seconds 1
    $clipboard = Find-ByName $dw "Clipboard"
    $printers = Find-ByName $dw "Printers"
    $drives = Find-ByName $dw "Drives"
    $audioPlay = Find-ByName $dw "Audio playback"
    $audioRec = Find-ByName $dw "Audio recording"
    $smartCards = Find-ByName $dw "Smart cards"
    $serialPorts = Find-ByName $dw "Serial ports"
    $usb = Find-ByName $dw "USB devices"
    Write-TestResult "Clipboard checkbox" ($null -ne $clipboard) "Not found"
    Write-TestResult "Printers checkbox" ($null -ne $printers) "Not found"
    Write-TestResult "Drives checkbox" ($null -ne $drives) "Not found"
    Write-TestResult "Audio playback checkbox" ($null -ne $audioPlay) "Not found"
    Write-TestResult "Audio recording checkbox" ($null -ne $audioRec) "Not found"
    Write-TestResult "Smart cards checkbox" ($null -ne $smartCards) "Not found"
    Write-TestResult "Serial ports checkbox" ($null -ne $serialPorts) "Not found"
    Write-TestResult "USB devices checkbox" ($null -ne $usb) "Not found"
} else {
    Write-TestResult "Redirections nav item found" $false "Not in tree"
}

# --- Test DW5: Navigate back to Connection ---
Write-Host "`n--- DW5: Navigate back to Connection ---"
$connNav = Find-ByName $dw "Connection"
if ($connNav) {
    Click-Element $connNav | Out-Null
    Start-Sleep -Seconds 1
    $connectBtn2 = Find-ByName $dw "Connect"
    Write-TestResult "Back to Connection page" ($null -ne $connectBtn2) "Connect button not found"
} else {
    Write-TestResult "Connection nav item found" $false "Not in tree"
}

# --- Test DW6: Titlebar shows device name ---
Write-Host "`n--- DW6: Titlebar shows device name ---"
$titleText = Find-ByName $dw "DWTest"
Write-TestResult "Device name in titlebar" ($null -ne $titleText) "'DWTest' not found in title"

# --- Test DW7: App still running ---
Write-Host "`n--- DW7: App health check ---"
Write-TestResult "App still running" (Assert-AppRunning) "App crashed"

# Cleanup
Close-WindowByButton $dw | Out-Null
Start-Sleep -Seconds 1
Stop-RemortApp
Remove-TempDataDir $dataDir

$results = Get-TestResults
Write-Host "`n=== Device Window Results: $($results.Passed) passed, $($results.Failed) failed ===" -ForegroundColor $(if ($results.Failed -eq 0) { "Green" } else { "Red" })
exit $results.Failed
