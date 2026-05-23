Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Dump-Tree {
    param($el, $depth = 0, $maxDepth = 5)
    if ($depth -gt $maxDepth) { return }
    $indent = '  ' * $depth
    $aid  = $el.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::AutomationIdProperty)
    $name = $el.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::NameProperty)
    $ct   = $el.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::ControlTypeProperty)
    $ctName = if ($ct) { $ct.ProgrammaticName -replace 'ControlType\.', '' } else { '?' }
    Write-Host "$indent[$ctName] aid='$aid' name='$name'"
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $child = $walker.GetFirstChild($el)
    while ($child -ne $null) {
        Dump-Tree $child ($depth+1) $maxDepth
        $child = $walker.GetNextSibling($child)
    }
}

$proc = Get-Process -Name HeroVirtualDesktop -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Host "App not running"; exit 1 }

$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
    [System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Host "Window not found"; exit 1 }

Write-Host "UIA Tree for $($proc.Name) (PID $($proc.Id)):"
Dump-Tree $win 0 5
