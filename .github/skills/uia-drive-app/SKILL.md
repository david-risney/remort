---
name: uia-drive-app
description: "Launch and drive the Remort WPF application using Windows UI Automation (UIA) via PowerShell. Use when: testing the app end-to-end, verifying UI behavior, automating manual test scenarios, clicking buttons, typing text, reading status, taking screenshots, diagnosing app crashes, analyzing crash dumps."
argument-hint: "Describe what to do in the app (e.g. 'connect to localhost and verify status shows Connected')"
---

# UI Automation — Drive Remort App

## When to Use

- Run manual test scenarios automatically
- Verify UI state after actions (status text, button enabled/disabled)
- Test the full COM interop pipeline with a real RDP control
- Reproduce and validate bug fixes
- Diagnose unexpected crashes — detect when the app disappears and analyze the crash dump

## Prerequisites

- The app must be built: `dotnet build Remort.sln`
- PowerShell 7+ (available as `pwsh`)
- No extra packages needed — uses .NET `System.Windows.Automation` built into Windows
- For crash analysis: `dotnet tool install --global dotnet-dump` (one-time)

## Crash Dump Setup (One-Time)

Configure Windows Error Reporting to save full crash dumps automatically.
Run this once from an **elevated** PowerShell prompt:

```powershell
$key = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Remort.exe"
New-Item -Path $key -Force | Out-Null
Set-ItemProperty -Path $key -Name "DumpType" -Value 2                # 2 = full dump
Set-ItemProperty -Path $key -Name "DumpFolder" -Value "%LOCALAPPDATA%\CrashDumps" -Type ExpandString
Set-ItemProperty -Path $key -Name "DumpCount" -Value 10
```

Also works for the `dotnet` host process (used by `dotnet run`):

```powershell
$key = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\dotnet.exe"
New-Item -Path $key -Force | Out-Null
Set-ItemProperty -Path $key -Name "DumpType" -Value 2
Set-ItemProperty -Path $key -Name "DumpFolder" -Value "%LOCALAPPDATA%\CrashDumps" -Type ExpandString
Set-ItemProperty -Path $key -Name "DumpCount" -Value 10
```

Verify setup:

```powershell
Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Remort.exe" -ErrorAction SilentlyContinue
Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\dotnet.exe" -ErrorAction SilentlyContinue
```

## Procedure

### 1. Launch the App

```powershell
$app = Start-Process -FilePath "dotnet" -ArgumentList "run","--project","src/Remort/Remort.csproj","--no-build" -PassThru
Start-Sleep -Seconds 3  # Wait for window to appear
```

### 2. Find the Main Window

```powershell
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$condition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Remort")
$window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condition)

if (-not $window) { throw "Remort window not found" }
```

### 3. Find Controls by AutomationId or Name

WPF exposes `x:Name` as `AutomationId`. Buttons expose `Content` as `Name`.

```powershell
function Find-Element {
    param(
        [System.Windows.Automation.AutomationElement]$Parent,
        [string]$AutomationId = "",
        [string]$Name = "",
        [string]$ClassName = ""
    )
    if ($AutomationId) {
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $AutomationId)
    } elseif ($Name) {
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, $Name)
    } elseif ($ClassName) {
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ClassNameProperty, $ClassName)
    }
    return $Parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}
```

### 4. Common Actions

**Type text into hostname field:**
```powershell
$hostnameBox = Find-Element -Parent $window -AutomationId "HostnameTextBox"
$hostnameBox.SetFocus()
$valuePattern = $hostnameBox.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$valuePattern.SetValue("davris-4")
```

**Click a button:**
```powershell
$connectBtn = Find-Element -Parent $window -Name "Connect"
$invokePattern = $connectBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
$invokePattern.Invoke()
```

**Read status bar text:**
```powershell
# The status TextBlock doesn't have x:Name — find it by walking the StatusBar
$statusBar = Find-Element -Parent $window -ClassName "StatusBar"
$textBlocks = $statusBar.FindAll(
    [System.Windows.Automation.TreeScope]::Descendants,
    New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text))
$statusText = $textBlocks[0].Current.Name
Write-Output "Status: $statusText"
```

**Check if a button is enabled:**
```powershell
$btn = Find-Element -Parent $window -Name "Connect"
$btn.Current.IsEnabled  # True/False
```

**Toggle a checkbox:**
```powershell
$checkbox = Find-Element -Parent $window -Name "Pin to desktop"
$togglePattern = $checkbox.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
$togglePattern.Toggle()
```

### 5. Known Remort UI Elements

| Element | Type | Find by | Notes |
|---------|------|---------|-------|
| Hostname field | TextBox | AutomationId `HostnameTextBox` | Use `ValuePattern.SetValue()` |
| Connect button | Button | Name `Connect` | Use `InvokePattern.Invoke()` |
| Disconnect button | Button | Name `Disconnect` | May be disabled |
| Max retries field | TextBox | Bound to `MaxRetryCount` | No x:Name — find by walking |
| Reconnect on sign-in | CheckBox | Name `Reconnect on sign-in` | `TogglePattern` |
| Pin to desktop | CheckBox | Name `Pin to desktop` | `TogglePattern` |
| Reconnect on desktop switch | CheckBox | Name `Reconnect on desktop switch` | `TogglePattern` |
| Desktop ComboBox | ComboBox | ClassName `ComboBox` | May be hidden |
| Status bar | StatusBar | ClassName `StatusBar` | Walk descendants for text |
| Theme button | Button | AutomationId `ThemeButton` | `InvokePattern` |
| RDP host | WindowsFormsHost | AutomationId `RdpHost` | ActiveX container |

### 6. Monitor Process Health

After each UIA action, check that the app process is still alive.
If it has exited unexpectedly, skip remaining UIA steps and jump to crash analysis.

```powershell
function Assert-AppRunning {
    param([System.Diagnostics.Process]$Process)
    $Process.Refresh()
    if ($Process.HasExited) {
        $exitCode = $Process.ExitCode
        $exitTime = $Process.ExitTime
        Write-Warning "App crashed! Exit code: $exitCode at $exitTime"
        return $false
    }
    return $true
}

# Use after each significant action:
if (-not (Assert-AppRunning $app)) {
    # App disappeared — proceed to crash dump analysis below
}
```

### 7. Cleanup

```powershell
if ($app -and !$app.HasExited) { $app.Kill(); $app.WaitForExit(5000) }
```

## Crash Dump Analysis

When the app disappears unexpectedly (process exits with non-zero code or `Assert-AppRunning` returns `$false`), follow these steps.

### 1. Locate the Crash Dump

```powershell
$dumpDir = Join-Path $env:LOCALAPPDATA "CrashDumps"
$recentDumps = Get-ChildItem -Path $dumpDir -Filter "*.dmp" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 5
$recentDumps | Format-Table Name, LastWriteTime, @{N='SizeMB';E={[math]::Round($_.Length/1MB,1)}}
```

If no dumps exist, WER LocalDumps may not be configured — see the setup section above. Also check if the crash was an orderly exit by inspecting the exit code.

### 2. Get the Managed Stack Trace

Use `dotnet-dump` to analyze the most recent dump:

```powershell
$dumpFile = $recentDumps[0].FullName
# Print the managed exception and stack trace non-interactively
dotnet-dump analyze $dumpFile -c "pe -lines" -c "clrstack" -c "exit"
```

Key `dotnet-dump` commands:

| Command | Purpose |
|---------|---------|
| `pe` | Print the current exception (if any) |
| `pe -lines` | Print exception with source line numbers |
| `clrstack` | Managed call stack of the current thread |
| `clrstack -all` | Managed call stacks of all threads |
| `threads` | List all managed threads |
| `dso` | Dump stack objects (local variables) |
| `dumpheap -stat` | Heap overview (for OOM crashes) |
| `eestack` | EE (execution engine) stacks for all threads |

### 3. COM Interop Crashes

RDP ActiveX crashes often show as `AccessViolationException` or `SEHException` in COM interop.
For these, also inspect the native stack:

```powershell
dotnet-dump analyze $dumpFile -c "pe" -c "clrstack -f" -c "exit"
```

The `-f` flag on `clrstack` shows full frame details including native-to-managed transitions.

### 4. Report Findings

After analysis, report:
- **Exit code** from the process
- **Exception type and message** from `pe`
- **Top of the call stack** from `clrstack`
- **Dump file path** for the user to share or investigate further

### 5. Cleanup Old Dumps

Crash dumps are large. Clean up after investigation:

```powershell
# List dumps older than 7 days
Get-ChildItem (Join-Path $env:LOCALAPPDATA "CrashDumps") -Filter "*.dmp" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } |
    Remove-Item -WhatIf  # Remove -WhatIf to actually delete
```

## Important Notes

- **Always `--no-build`** when launching — the app should be pre-built.
- **`Start-Sleep`** after actions that trigger async work (Connect, Disconnect).
- **Credential prompts** from RDP are native Windows dialogs, not WPF — UIA can find them but cannot type passwords programmatically via `ValuePattern`. They block the COM flow.
- **Run terminal commands sequentially** — UIA calls are synchronous and must happen in order.
