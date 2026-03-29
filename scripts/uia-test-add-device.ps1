#!/usr/bin/env pwsh
# UIA test: Verify Add Device dialog shows OK/Cancel and completes
$ErrorActionPreference = 'Stop'

Write-Host "Launching Remort..."
$app = Start-Process -FilePath "dotnet" -ArgumentList "run","--project","src/Remort/Remort.csproj","--no-build" -PassThru
Start-Sleep -Seconds 6

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Remort")

# Retry finding the window a few times
$window = $null
for ($i = 0; $i -lt 5; $i++) {
    $window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
    if ($window) { break }
    Start-Sleep -Seconds 2
}
if (-not $window) { throw "Remort window not found" }

# Find and click Add Device button
$addBtn = $null
$all = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    [System.Windows.Automation.Condition]::TrueCondition)
foreach ($el in $all) {
    if ($el.Current.Name -eq "Add Device") {
        try {
            $invoke = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            $invoke.Invoke()
            Write-Host "Clicked 'Add Device'"
            break
        } catch { }
    }
}

Start-Sleep -Seconds 2

# Look for OK and Cancel buttons in the dialog
$okBtn = $null
$cancelBtn = $null
$all2 = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    [System.Windows.Automation.Condition]::TrueCondition)
foreach ($el in $all2) {
    $name = $el.Current.Name
    $ctrlType = $el.Current.ControlType.ProgrammaticName
    if ($name -eq "OK" -and $ctrlType -eq "ControlType.Button") { $okBtn = $el }
    if ($name -eq "Cancel" -and $ctrlType -eq "ControlType.Button") { $cancelBtn = $el }
}

if ($okBtn) { Write-Host "PASS: Found OK button" -ForegroundColor Green }
else { Write-Host "FAIL: OK button not found" -ForegroundColor Red }

if ($cancelBtn) { Write-Host "PASS: Found Cancel button" -ForegroundColor Green }
else { Write-Host "FAIL: Cancel button not found" -ForegroundColor Red }

# Cleanup
if ($app -and !$app.HasExited) { $app.Kill(); $app.WaitForExit(5000) }
