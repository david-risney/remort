#!/usr/bin/env pwsh
# E2E tests: Persistence, favorites, validation, context menu, settings
# Prereq: app must be built (dotnet build Remort.sln)

$ErrorActionPreference = 'Continue'
. "$PSScriptRoot/uia-helpers.ps1"

Write-Host "`n=== E2E: Persistence & Advanced ===" -ForegroundColor Cyan

$dataDir = New-TempDataDir
Write-Host "Using temp data dir: $dataDir"

# ===== Phase 1: Add devices, verify persistence across restart =====

Write-Host "`n--- P1: Add device and verify persistence ---"
$app = Start-RemortApp -DataDir $dataDir
$window = Find-Window "Remort"
if (-not $window) { Write-Host "ABORT" -ForegroundColor Red; exit 1 }

$added = Add-TestDevice $window "PersistDev" "persist.local"
Write-TestResult "Device added" $added "Add failed"

# Verify card exists
$card = Find-ByName $window "PersistDev"
Write-TestResult "Card visible before restart" ($null -ne $card) "Card not found"

# Kill and restart
Write-Host "`nRestarting app..."
Stop-RemortApp
Start-Sleep -Seconds 2
$app = Start-RemortApp -DataDir $dataDir
$window = Find-Window "Remort"
if (-not $window) { Write-Host "ABORT after restart" -ForegroundColor Red; exit 1 }

Start-Sleep -Seconds 2
$cardAfter = Find-ByName $window "PersistDev"
Write-TestResult "Card persisted after restart" ($null -ne $cardAfter) "Device not found after restart"

# ===== Phase 2: Add Device validation =====

Write-Host "`n--- P2: Add Device validation ---"
$addBtn = Find-ByName $window "Add Device"
if ($addBtn) {
    Click-Element $addBtn | Out-Null
    Start-Sleep -Seconds 1

    # Try OK with empty fields
    $okBtn = Find-ByName $window "OK"
    if ($okBtn) {
        Click-Element $okBtn | Out-Null
        Start-Sleep -Seconds 1
        # Dialog should still be open (validation failed)
        $stillHasOk = Find-ByName $window "OK"
        Write-TestResult "Empty fields: dialog stays open" ($null -ne $stillHasOk) "Dialog closed with empty fields"
    }

    # Cancel
    $cancelBtn = Find-ByName $window "Cancel"
    if ($cancelBtn) { Click-Element $cancelBtn | Out-Null; Start-Sleep -Seconds 1 }
}

# ===== Phase 3: Multiple devices =====

Write-Host "`n--- P3: Multiple devices ---"
$added2 = Add-TestDevice $window "Device2" "dev2.local"
Write-TestResult "Second device added" $added2 "Add failed"

$card1 = Find-ByName $window "PersistDev"
$card2 = Find-ByName $window "Device2"
Write-TestResult "Both cards visible" (($null -ne $card1) -and ($null -ne $card2)) "One or both cards missing"

# ===== Phase 4: Context menu — Remove Device =====

Write-Host "`n--- P4: Remove device via context menu ---"
$cardToRemove = Find-ByName $window "Device2"
if ($cardToRemove) {
    RightClick-Element $cardToRemove | Out-Null
    Start-Sleep -Seconds 1

    $removeItem = Find-ByName $window "Remove Device"
    if ($removeItem) {
        Write-TestResult "Context menu has Remove Device" $true ""
        Click-Element $removeItem | Out-Null
        Start-Sleep -Seconds 2

        $cardGone = $null -eq (Find-ByName $window "Device2")
        Write-TestResult "Device2 removed from list" $cardGone "Card still present"
    } else {
        Write-TestResult "Remove Device in context menu" $false "Menu item not found"
    }
} else {
    Write-TestResult "Card available for right-click" $false "Device2 card not found"
}

# ===== Phase 5: Favorites flow =====

Write-Host "`n--- P5: Favorites ---"
# Toggle favorite via context menu
$cardForFav = Find-ByName $window "PersistDev"
if ($cardForFav) {
    RightClick-Element $cardForFav | Out-Null
    Start-Sleep -Seconds 1
    $toggleFav = Find-ByName $window "Toggle Favorite"
    if ($toggleFav) {
        Write-TestResult "Context menu has Toggle Favorite" $true ""
        Click-Element $toggleFav | Out-Null
        Start-Sleep -Seconds 1

        # Navigate to Favorites page
        $favNav = Find-ByName $window "Favorites"
        if ($favNav) {
            Click-Element $favNav | Out-Null
            Start-Sleep -Seconds 1
            $favCard = Find-ByName $window "PersistDev"
            Write-TestResult "Favorited device on Favorites page" ($null -ne $favCard) "Not found on Favorites"
        }
    } else {
        Write-TestResult "Toggle Favorite in context menu" $false "Not found"
    }
} else {
    Write-TestResult "Card available for favorite" $false "Not found"
}

# ===== Phase 6: Settings page =====

Write-Host "`n--- P6: Settings page content ---"
$settingsNav = Find-ByName $window "Settings"
if ($settingsNav) {
    Click-Element $settingsNav | Out-Null
    Start-Sleep -Seconds 1

    $lightRadio = Find-ByName $window "Light"
    $darkRadio = Find-ByName $window "Dark"
    $systemRadio = Find-ByName $window "System"
    $devboxToggle = Find-ByName $window "Enable automatic device discovery"
    $versionText = Find-ByName $window "About"

    Write-TestResult "Light theme radio" ($null -ne $lightRadio) "Not found"
    Write-TestResult "Dark theme radio" ($null -ne $darkRadio) "Not found"
    Write-TestResult "System theme radio" ($null -ne $systemRadio) "Not found"
    Write-TestResult "DevBox discovery toggle" ($null -ne $devboxToggle) "Not found"
    Write-TestResult "About section" ($null -ne $versionText) "Not found"
} else {
    Write-TestResult "Settings nav item" $false "Not found"
}

# ===== Phase 7: App health =====
Write-Host "`n--- P7: Final health check ---"
Write-TestResult "App still running" (Assert-AppRunning) "App crashed"

# Cleanup
Stop-RemortApp
Remove-TempDataDir $dataDir

$results = Get-TestResults
Write-Host "`n=== Persistence & Advanced Results: $($results.Passed) passed, $($results.Failed) failed ===" -ForegroundColor $(if ($results.Failed -eq 0) { "Green" } else { "Red" })
exit $results.Failed
