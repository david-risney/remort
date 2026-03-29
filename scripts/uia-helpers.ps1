#!/usr/bin/env pwsh
# Shared UIA helpers for E2E test scripts

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

if (-not ([System.Management.Automation.PSTypeName]'UiaClick').Type) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public class UiaClick {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);
    public static void LeftClick(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        mouse_event(0x0002, 0, 0, 0, IntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, IntPtr.Zero);
    }
    public static void RightClick(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        mouse_event(0x0008, 0, 0, 0, IntPtr.Zero);
        mouse_event(0x0010, 0, 0, 0, IntPtr.Zero);
    }
}
"@
}

$script:_passed = 0
$script:_failed = 0

function Write-TestResult($name, $pass, $detail) {
    if ($pass) {
        Write-Host "  PASS: $name" -ForegroundColor Green
        $script:_passed++
    } else {
        Write-Host "  FAIL: $name — $detail" -ForegroundColor Red
        $script:_failed++
    }
}

function Get-TestResults { return @{ Passed = $script:_passed; Failed = $script:_failed } }

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

function Find-ByAutoId($parent, $id) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Find-AllByAutoId($parent, $id) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $parent.FindAll([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Click-Element($el) {
    try {
        $invoke = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        return $true
    } catch { }
    $rect = $el.Current.BoundingRectangle
    if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
        [UiaClick]::LeftClick([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2))
        return $true
    }
    return $false
}

function RightClick-Element($el) {
    $rect = $el.Current.BoundingRectangle
    if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
        [UiaClick]::RightClick([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2))
        return $true
    }
    return $false
}

function Type-InTextBox($el, $text) {
    $el.SetFocus()
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.SendKeys]::SendWait("^a")
    Start-Sleep -Milliseconds 100
    # Escape special SendKeys chars
    $escaped = $text -replace '([+^%~{}()\[\]])', '{$1}'
    [System.Windows.Forms.SendKeys]::SendWait($escaped)
    Start-Sleep -Milliseconds 200
}

function Find-Window($name, $timeoutMs = 5000) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $end = [DateTime]::Now.AddMilliseconds($timeoutMs)
    while ([DateTime]::Now -lt $end) {
        $w = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $c)
        if ($w) { return $w }
        Start-Sleep -Milliseconds 500
    }
    return $null
}

function Assert-AppRunning() {
    $proc = Get-Process -Name Remort -ErrorAction SilentlyContinue | Select-Object -First 1
    return ($null -ne $proc -and !$proc.HasExited)
}

function Focus-RemortWindow() {
    $proc = Get-Process -Name Remort -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($proc -and $proc.MainWindowHandle -ne [IntPtr]::Zero) {
        if (-not ([System.Management.Automation.PSTypeName]'WinFocus').Type) {
            Add-Type @"
using System; using System.Runtime.InteropServices;
public class WinFocus {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@
        }
        [WinFocus]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null  # SW_RESTORE
        [WinFocus]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
        Start-Sleep -Milliseconds 500
    }
}

function Start-RemortApp([string]$DataDir = "") {
    Get-Process -Name Remort -ErrorAction SilentlyContinue | Stop-Process -Force 2>$null
    Start-Sleep -Seconds 1

    if ($DataDir) {
        $env:REMORT_DATA_DIR = $DataDir
    }

    $app = Start-Process -FilePath "dotnet" -ArgumentList "run","--project","src/Remort/Remort.csproj","--no-build" -PassThru
    Start-Sleep -Seconds 6

    # Don't remove env var until after app has started and read it

    Focus-RemortWindow
    return $app
}

function New-TempDataDir() {
    $dir = Join-Path ([System.IO.Path]::GetTempPath()) "remort-test-$([Guid]::NewGuid().ToString('N').Substring(0,8))"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    return $dir
}

function Remove-TempDataDir($dir) {
    if ($dir -and (Test-Path $dir)) {
        Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Stop-RemortApp() {
    Get-Process -Name Remort -ErrorAction SilentlyContinue | Stop-Process -Force 2>$null
    Start-Sleep -Seconds 1
}

function Close-WindowByButton($window) {
    $closeCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "TitleBarCloseButton")
    $allBtns = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $closeCond)
    if ($allBtns.Count -gt 0) {
        Click-Element $allBtns[0] | Out-Null
        Start-Sleep -Seconds 1
        return $true
    }
    return $false
}

function Add-TestDevice($window, $name, $hostname) {
    $addBtn = Find-ByName $window "Add Device"
    if (-not $addBtn) { return $false }
    Click-Element $addBtn | Out-Null
    Start-Sleep -Seconds 2

    # Dialog is a child window within the main window (FluentWindow modal)
    $nameBox = Find-ByAutoId $window "NameTextBox"
    $hostBox = Find-ByAutoId $window "HostnameTextBox"
    if (-not $nameBox -or -not $hostBox) { return $false }

    Type-InTextBox $nameBox $name
    Type-InTextBox $hostBox $hostname

    $okBtn = Find-ByName $window "OK"
    if (-not $okBtn) { return $false }
    Click-Element $okBtn | Out-Null
    Start-Sleep -Seconds 2
    return $true
}
