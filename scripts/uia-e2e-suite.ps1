#!/usr/bin/env pwsh
# Comprehensive E2E test suite for Remort UI using Windows UI Automation
# Validates: app launch, navigation, add device, device window, cleanup

param(
    [switch]$KeepRunning
)

$ErrorActionPreference = 'Continue'
$script:passed = 0
$script:failed = 0
$script:app = $null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

# --- Click helper (Wpf.Ui NavigationViewItems don't support InvokePattern) ---
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class UiaClick {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);
    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        mouse_event(0x0002, 0, 0, 0, IntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, IntPtr.Zero);
    }
}
"@

function Write-TestResult($name, $pass, $detail) {
    if ($pass) {
        Write-Host "  PASS: $name" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "  FAIL: $name — $detail" -ForegroundColor Red
        $script:failed++
    }
}

function Find-ByName($parent, $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Find-AllByName($parent, $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $parent.FindAll([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Click-Element($el) {
    # Try InvokePattern first
    try {
        $invoke = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        return $true
    } catch { }
    # Fall back to mouse click
    $rect = $el.Current.BoundingRectangle
    if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
        [UiaClick]::Click([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2))
        return $true
    }
    return $false
}

function Find-Window($name) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    for ($i = 0; $i -lt 10; $i++) {
        $w = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $c)
        if ($w) { return $w }
        Start-Sleep -Milliseconds 500
    }
    return $null
}

function Assert-AppRunning() {
    $proc = Get-Process -Name Remort -ErrorAction SilentlyContinue | Select-Object -First 1
    return ($proc -and !$proc.HasExited)
}

# ==============================================================================
# LAUNCH
# ==============================================================================
Write-Host "`n=== Launching Remort ===" -ForegroundColor Cyan
Get-Process -Name Remort -ErrorAction SilentlyContinue | Stop-Process -Force 2>$null
Start-Sleep -Seconds 1
$script:app = Start-Process -FilePath "dotnet" -ArgumentList "run","--project","src/Remort/Remort.csproj","--no-build" -PassThru
Start-Sleep -Seconds 6

# --- Test 1: App launches without crash ---
Write-Host "`n--- Test 1: App launches without crash ---"
$running = Assert-AppRunning
Write-TestResult "App process is running" $running "Process not found or exited"
if (-not $running) {
    Write-Host "`nApp crashed on launch. Aborting." -ForegroundColor Red
    exit 1
}

$window = Find-Window "Remort"
Write-TestResult "Main window found" ($null -ne $window) "Window with title 'Remort' not found"
if (-not $window) {
    Write-Host "`nMain window not found. Aborting." -ForegroundColor Red
    if ($script:app -and !$script:app.HasExited) { $script:app.Kill() }
    exit 1
}

# --- Test 2: Devices page loads by default ---
Write-Host "`n--- Test 2: Devices page loads by default ---"
Start-Sleep -Seconds 1
$devicesHeading = Find-ByName $window "Devices"
Write-TestResult "Devices heading visible" ($null -ne $devicesHeading) "No 'Devices' text found in content area"

# --- Test 3: Add Device button is visible ---
Write-Host "`n--- Test 3: Add Device button is visible ---"
$addBtn = Find-ByName $window "Add Device"
Write-TestResult "Add Device button visible" ($null -ne $addBtn) "No 'Add Device' button found"

# --- Test 4: Navigate to Favorites ---
Write-Host "`n--- Test 4: Navigate to Favorites ---"
$favNav = Find-ByName $window "Favorites"
if ($favNav) {
    Click-Element $favNav | Out-Null
    Start-Sleep -Seconds 1
    $favHeading = Find-ByName $window "Favorites"
    Write-TestResult "Favorites page shows" ($null -ne $favHeading) "Favorites content not found after click"
} else {
    Write-TestResult "Favorites nav item found" $false "Nav item not in tree"
}

# --- Test 5: Navigate to Settings ---
Write-Host "`n--- Test 5: Navigate to Settings ---"
$settingsNav = Find-ByName $window "Settings"
if ($settingsNav) {
    Click-Element $settingsNav | Out-Null
    Start-Sleep -Seconds 1
    $aboutText = Find-ByName $window "Remort"
    $lightRadio = Find-ByName $window "Light"
    Write-TestResult "Settings page shows" (($null -ne $aboutText) -and ($null -ne $lightRadio)) "Settings page content not fully rendered"
} else {
    Write-TestResult "Settings nav item found" $false "Nav item not in tree"
}

# --- Test 6: Navigate back to Devices ---
Write-Host "`n--- Test 6: Navigate back to Devices ---"
$devNav = Find-AllByName $window "Devices"
$navItem = $null
foreach ($el in $devNav) {
    if ($el.Current.ClassName -eq "ItemsControlItem" -or $el.Current.ControlType.ProgrammaticName -eq "ControlType.DataItem") {
        $navItem = $el; break
    }
}
if ($navItem) {
    Click-Element $navItem | Out-Null
    Start-Sleep -Seconds 1
    $addBtnAgain = Find-ByName $window "Add Device"
    Write-TestResult "Devices page restored" ($null -ne $addBtnAgain) "Add Device button not found after re-navigation"
} else {
    Write-TestResult "Devices nav item clickable" $false "Could not find Devices nav item to click"
}

# --- Test 7: Add Device dialog has OK/Cancel ---
Write-Host "`n--- Test 7: Add Device dialog has OK/Cancel ---"
$addBtn2 = Find-ByName $window "Add Device"
if ($addBtn2) {
    Click-Element $addBtn2 | Out-Null
    Start-Sleep -Seconds 2

    $okBtn = Find-ByName $window "OK"
    $cancelBtn = Find-ByName $window "Cancel"
    Write-TestResult "OK button found" ($null -ne $okBtn) "No OK button in dialog"
    Write-TestResult "Cancel button found" ($null -ne $cancelBtn) "No Cancel button in dialog"
} else {
    Write-TestResult "Add Device button clickable" $false "Button not found"
}

# --- Test 8: Cancel closes dialog without adding ---
Write-Host "`n--- Test 8: Cancel closes dialog ---"
$cancelBtn2 = Find-ByName $window "Cancel"
if ($cancelBtn2) {
    Click-Element $cancelBtn2 | Out-Null
    Start-Sleep -Seconds 1
    Write-TestResult "App still running after cancel" (Assert-AppRunning) "App crashed after cancel"
} else {
    Write-TestResult "Cancel button clickable" $false "Button not found"
}

# --- Test 9 & 10: OK with valid input adds a device card ---
Write-Host "`n--- Test 9: Add device with valid input ---"
$addBtn3 = Find-ByName $window "Add Device"
if ($addBtn3) {
    Click-Element $addBtn3 | Out-Null
    Start-Sleep -Seconds 1

    # Find and fill Name textbox
    $nameBox = $null
    $editCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
    $allEdits = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
    foreach ($edit in $allEdits) {
        if ($edit.Current.AutomationId -eq "NameTextBox") { $nameBox = $edit }
    }

    $hostBox = $null
    foreach ($edit in $allEdits) {
        if ($edit.Current.AutomationId -eq "HostnameTextBox") { $hostBox = $edit }
    }

    if ($nameBox -and $hostBox) {
        $nameBox.SetFocus()
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait("^a")
        [System.Windows.Forms.SendKeys]::SendWait("TestDevice")
        Start-Sleep -Milliseconds 200

        $hostBox.SetFocus()
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait("^a")
        [System.Windows.Forms.SendKeys]::SendWait("test.local")
        Start-Sleep -Milliseconds 200

        $okBtn2 = Find-ByName $window "OK"
        if ($okBtn2) {
            Click-Element $okBtn2 | Out-Null
            Start-Sleep -Seconds 2

            Write-TestResult "App running after OK" (Assert-AppRunning) "App crashed after OK"

            # Test 10: Device card appears
            $card = Find-ByName $window "TestDevice"
            Write-TestResult "Device card 'TestDevice' appears" ($null -ne $card) "Card not found in list"
        } else {
            Write-TestResult "OK button found for submit" $false "OK button not available"
        }
    } else {
        Write-TestResult "TextBoxes found" $false "Name=$($null -ne $nameBox) Host=$($null -ne $hostBox)"
    }
} else {
    Write-TestResult "Add Device for submit" $false "Button not found"
}

# --- Test 11: Click device card opens device window ---
Write-Host "`n--- Test 11: Click device card opens device window ---"
$card2 = Find-ByName $window "TestDevice"
if ($card2) {
    Click-Element $card2 | Out-Null
    Start-Sleep -Seconds 3
    $deviceWindow = Find-Window "TestDevice"
    Write-TestResult "Device window opened" ($null -ne $deviceWindow) "Window with title 'TestDevice' not found"
} else {
    Write-TestResult "Device card clickable" $false "Card not found"
    $deviceWindow = $null
}

# --- Test 12: Device window shows Connection page ---
Write-Host "`n--- Test 12: Device window shows Connection page ---"
if ($deviceWindow) {
    $connectionText = Find-ByName $deviceWindow "Connection"
    Write-TestResult "Connection page visible" ($null -ne $connectionText) "No 'Connection' text in device window"
} else {
    Write-TestResult "Device window available" $false "Skipped — no device window"
}

# --- Test 13: Device window has Connect button ---
Write-Host "`n--- Test 13: Connect button in device window ---"
if ($deviceWindow) {
    $connectBtn = Find-ByName $deviceWindow "Connect"
    Write-TestResult "Connect button visible" ($null -ne $connectBtn) "No 'Connect' button found"
} else {
    Write-TestResult "Device window available" $false "Skipped — no device window"
}

# --- Test 13a: Click Connect does not crash ---
Write-Host "`n--- Test 13a: Click Connect does not crash ---"
if ($deviceWindow) {
    $connectBtn2 = Find-ByName $deviceWindow "Connect"
    if ($connectBtn2) {
        Click-Element $connectBtn2 | Out-Null
        Start-Sleep -Seconds 3
        $stillRunning = Assert-AppRunning
        Write-TestResult "App running after Connect click" $stillRunning "App crashed after clicking Connect"

        # Check status updated (should show Connecting or error, not blank)
        if ($stillRunning) {
            $deviceWindow = Find-Window "TestDevice"
            if ($deviceWindow) {
                $statusText = Find-ByName $deviceWindow "Disconnected"
                $connectingText = Find-ByName $deviceWindow "Connecting"
                $hasStatus = ($null -ne $statusText) -or ($null -ne $connectingText)
                Write-TestResult "Status label updated after Connect" $hasStatus "No status text found"
            }
        }
    } else {
        Write-TestResult "Connect button clickable" $false "Button not found"
    }
} else {
    Write-TestResult "Device window available" $false "Skipped"
}

# --- Test 14: Close device window cleanly ---
Write-Host "`n--- Test 14: Close device window ---"
$deviceWindow = Find-Window "TestDevice"
if ($deviceWindow) {
    $closeBtn = $null
    $closeCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, "TitleBarCloseButton")
    $allBtns = $deviceWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $closeCond)
    if ($allBtns.Count -gt 0) { $closeBtn = $allBtns[0] }
    if ($closeBtn) {
        Click-Element $closeBtn | Out-Null
        Start-Sleep -Seconds 2
        $deviceWindowGone = $null -eq (Find-Window "TestDevice")
        Write-TestResult "Device window closed" $deviceWindowGone "Window still present"
    } else {
        Write-TestResult "Close button found" $false "TitleBarCloseButton not found"
    }
} else {
    Write-TestResult "Device window found for close" $false "Window already gone"
}

# --- Test 15: App closes cleanly ---
Write-Host "`n--- Test 15: App closes cleanly ---"
$window = Find-Window "Remort"
if ($window) {
    $mainClose = $null
    $closeCond2 = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, "TitleBarCloseButton")
    $allBtns2 = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $closeCond2)
    if ($allBtns2.Count -gt 0) { $mainClose = $allBtns2[0] }
    if ($mainClose) {
        Click-Element $mainClose | Out-Null
        Start-Sleep -Seconds 2
        $stillRunning = Assert-AppRunning
        Write-TestResult "App exited cleanly" (-not $stillRunning) "Remort process still running"
    } else {
        Write-TestResult "Main close button found" $false "TitleBarCloseButton not found"
    }
} else {
    Write-TestResult "Main window found for close" $false "Window already gone"
}

# ==============================================================================
# RESULTS
# ==============================================================================
Write-Host "`n=== Results: $($script:passed) passed, $($script:failed) failed ===" -ForegroundColor $(if ($script:failed -eq 0) { "Green" } else { "Red" })

# Cleanup
if (-not $KeepRunning) {
    Get-Process -Name Remort -ErrorAction SilentlyContinue | Stop-Process -Force 2>$null
}

exit $script:failed
