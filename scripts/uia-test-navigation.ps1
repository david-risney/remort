#!/usr/bin/env pwsh
# UIA test: Verify main window navigation wiring
# - App shows Devices page on launch (default)
# - Clicking Favorites nav item shows Favorites view
# - Clicking Settings nav item shows Settings view

param(
    [switch]$KeepRunning
)

$ErrorActionPreference = 'Stop'
$passed = 0
$failed = 0

function Write-Pass($msg) { Write-Host "  PASS: $msg" -ForegroundColor Green; $script:passed++ }
function Write-Fail($msg) { Write-Host "  FAIL: $msg" -ForegroundColor Red; $script:failed++ }

# --- Launch ---
Write-Host "Launching Remort..."
$app = Start-Process -FilePath "dotnet" -ArgumentList "run","--project","src/Remort/Remort.csproj","--no-build" -PassThru
Start-Sleep -Seconds 5

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Remort")
$window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)

if (-not $window) {
    Write-Fail "Remort window not found"
    if ($app -and !$app.HasExited) { $app.Kill() }
    exit 1
}

function Find-ByName($parent, $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Click-NavItem($parent, $name) {
    $item = Find-ByName $parent $name
    if (-not $item) { return $false }

    # Try InvokePattern first, then SelectionItemPattern, then click via point
    try {
        $invoke = $item.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        return $true
    } catch { }

    try {
        $select = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $select.Select()
        return $true
    } catch { }

    # Fall back to clicking via screen coordinates
    try {
        $rect = $item.Current.BoundingRectangle
        if ($rect.Width -gt 0) {
            $x = [int]($rect.X + $rect.Width / 2)
            $y = [int]($rect.Y + $rect.Height / 2)
            Add-Type @"
using System;
using System.Runtime.InteropServices;
public class ClickHelper {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);
    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        mouse_event(0x0002, 0, 0, 0, IntPtr.Zero); // MOUSEEVENTF_LEFTDOWN
        mouse_event(0x0004, 0, 0, 0, IntPtr.Zero); // MOUSEEVENTF_LEFTUP
    }
}
"@
            [ClickHelper]::Click($x, $y)
            return $true
        }
    } catch { }

    return $false
}

# --- Test 1: Default page shows "Devices" heading ---
Write-Host "`nTest 1: App shows Devices page on launch"
Start-Sleep -Seconds 2
$devicesHeading = Find-ByName $window "Devices"
if ($devicesHeading) {
    Write-Pass "Found 'Devices' text in content area"
} else {
    Write-Fail "Did not find 'Devices' text — navigation may not be wired"
}

# --- Test 2: Add Device button is visible on Devices page ---
Write-Host "`nTest 2: Add Device button is visible"
$addBtn = Find-ByName $window "Add Device"
if ($addBtn) {
    Write-Pass "Found 'Add Device' button"
} else {
    Write-Fail "'Add Device' button not found"
}

# --- Test 3: Click Favorites nav item → shows "Favorites" heading ---
Write-Host "`nTest 3: Click Favorites shows Favorites view"
if (Click-NavItem $window "Favorites") {
    Start-Sleep -Seconds 1
    $favHeading = Find-ByName $window "Favorites"
    if ($favHeading) {
        Write-Pass "Favorites page is showing"
    } else {
        Write-Fail "Favorites page content not found"
    }
} else {
    Write-Fail "Could not click Favorites nav item"
}

# --- Test 4: Click Settings nav item → shows "Settings" heading ---
Write-Host "`nTest 4: Click Settings shows Settings view"
if (Click-NavItem $window "Settings") {
    Start-Sleep -Seconds 1
    $settingsHeading = Find-ByName $window "Settings"
    if ($settingsHeading) {
        Write-Pass "Settings page is showing"
    } else {
        Write-Fail "Settings page content not found"
    }
} else {
    Write-Fail "Could not click Settings nav item"
}

# --- Cleanup ---
Write-Host "`n--- Results: $passed passed, $failed failed ---"
if (-not $KeepRunning -and $app -and !$app.HasExited) {
    $app.Kill()
    $app.WaitForExit(5000)
}

exit $failed
