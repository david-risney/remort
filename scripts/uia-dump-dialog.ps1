#!/usr/bin/env pwsh
# Dump UIA tree of the Add Device dialog
. "$PSScriptRoot/uia-helpers.ps1"

$app = Start-RemortApp
$w = Find-Window "Remort"
if (-not $w) { Write-Host "No window"; exit 1 }
Focus-RemortWindow
Start-Sleep 1

# Click Add Device
$ab = Find-ByName $w "Add Device"
if ($ab) { Click-Element $ab | Out-Null } else { Write-Host "No Add Device btn"; exit 1 }
Start-Sleep 2

# Find all windows for the Remort process
$root = [System.Windows.Automation.AutomationElement]::RootElement
$remortProc = Get-Process -Name Remort -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $remortProc) { Write-Host "No Remort process"; exit 1 }

$pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $remortProc.Id)
$remortWindows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)
Write-Host "=== Windows for PID $($remortProc.Id) ($($remortWindows.Count) windows) ==="
foreach ($rw in $remortWindows) {
    Write-Host "`n  Window: '$($rw.Current.Name)' Class=$($rw.Current.ClassName)"
    $descs = $rw.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($d in $descs) {
        $n = $d.Current.Name; $aid = $d.Current.AutomationId; $ct = $d.Current.ControlType.ProgrammaticName; $cn = $d.Current.ClassName
        if ($n -or $aid) { Write-Host "    [$ct] Name='$n' AutoId='$aid' Class='$cn'" }
    }
}

Stop-RemortApp
