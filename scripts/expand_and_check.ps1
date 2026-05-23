Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$desktop = [System.Windows.Automation.AutomationElement]::RootElement
$nameCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "MainWindowV2")
$appWin = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Children, $nameCond)
if (-not $appWin) { Write-Host "no window"; exit }
Write-Host "Found window: '$($appWin.Current.Name)'"

$btnCT  = [System.Windows.Automation.ControlType]::Button
$ctCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $btnCT)
$nCond  = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Character Explorer")
$and    = New-Object System.Windows.Automation.AndCondition($ctCond, $nCond)
$btn    = $appWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $and)
if (-not $btn) { Write-Host "no expander"; exit }
Write-Host "Found button: '$($btn.Current.Name)'"

# Try TogglePattern
$toggled = $false
try {
    $tp = $btn.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $tp.Toggle()
    $toggled = $true
    Write-Host "Toggled via TogglePattern"
} catch {
    Write-Host "TogglePattern failed: $_"
}

if (-not $toggled) {
    try {
        $ip = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $ip.Invoke()
        Write-Host "Invoked via InvokePattern"
    } catch {
        Write-Host "InvokePattern also failed: $_"
    }
}

Start-Sleep -Seconds 2

Write-Host "All buttons after toggle:"
$allBtns = $appWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, $ctCond)
foreach ($b in $allBtns) {
    Write-Host "  '$($b.Current.Name)'  id='$($b.Current.AutomationId)'"
}
