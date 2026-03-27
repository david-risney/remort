---
name: uia-drive-app
description: "Launch and drive the Remort WPF application using Windows UI Automation (UIA) via PowerShell. Use when: testing the app end-to-end, verifying UI behavior, automating manual test scenarios, clicking buttons, typing text, reading status, taking screenshots."
argument-hint: "Describe what to do in the app (e.g. 'connect to localhost and verify status shows Connected')"
---

# UI Automation — Drive Remort App

## When to Use

- Run manual test scenarios automatically
- Verify UI state after actions (status text, button enabled/disabled)
- Test the full COM interop pipeline with a real RDP control
- Reproduce and validate bug fixes

## Prerequisites

- The app must be built: `dotnet build Remort.sln`
- PowerShell 7+ (available as `pwsh`)
- No extra packages needed — uses .NET `System.Windows.Automation` built into Windows

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

### 6. Cleanup

```powershell
if ($app -and !$app.HasExited) { $app.Kill(); $app.WaitForExit(5000) }
```

## Important Notes

- **Always `--no-build`** when launching — the app should be pre-built.
- **`Start-Sleep`** after actions that trigger async work (Connect, Disconnect).
- **Credential prompts** from RDP are native Windows dialogs, not WPF — UIA can find them but cannot type passwords programmatically via `ValuePattern`. They block the COM flow.
- **Run terminal commands sequentially** — UIA calls are synchronous and must happen in order.
