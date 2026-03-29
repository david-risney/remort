#!/usr/bin/env pwsh
# UIA script: set hostname to davris-4 and click Connect
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Util {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Remort")
$window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)

if (-not $window) { throw "Remort window not found" }
Write-Host "Found window: $($window.Current.Name)"

# Bring to foreground
$proc = Get-Process -Name Remort -ErrorAction SilentlyContinue | Select-Object -First 1
if ($proc) {
    [Win32Util]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null   # SW_RESTORE
    [Win32Util]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 500
}

# Find and fill hostname
$hCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "HostnameTextBox")
$hBox = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $hCond)
if (-not $hBox) { throw "HostnameTextBox not found" }
$hBox.SetFocus()
Start-Sleep -Milliseconds 300
[System.Windows.Forms.SendKeys]::SendWait("^a")
Start-Sleep -Milliseconds 100
[System.Windows.Forms.SendKeys]::SendWait("davris{-}4")
Start-Sleep -Milliseconds 500
Write-Host "Typed hostname: davris-4"

# Click Connect
$btnCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Connect")
$connectBtn = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
if (-not $connectBtn) { throw "Connect button not found" }
Write-Host "Connect enabled: $($connectBtn.Current.IsEnabled)"
$invokePattern = $connectBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
$invokePattern.Invoke()
Write-Host "Connect clicked!"

Start-Sleep -Seconds 3

# Check process health
$proc.Refresh()
if ($proc.HasExited) {
    Write-Warning "App crashed! Exit code: $($proc.ExitCode)"
    exit 1
}

# Read status
$sCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ClassNameProperty, "StatusBar")
$statusBar = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $sCond)
if ($statusBar) {
    $tCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $texts = $statusBar.FindAll([System.Windows.Automation.TreeScope]::Descendants, $tCond)
    foreach ($t in $texts) { Write-Host "Status: '$($t.Current.Name)'" }
}
Write-Host "Done."
