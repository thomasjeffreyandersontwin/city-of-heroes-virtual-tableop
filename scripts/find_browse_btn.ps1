$pkgRoot = "C:\hero-desktop\city-of-heroes-virtual-tabletop\HerovirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.UITest\packages"
Add-Type -Path "$pkgRoot\Interop.UIAutomationClient.10.19041.0\lib\net45\Interop.UIAutomationClient.dll"
Add-Type -Path "$pkgRoot\FlaUI.Core.4.0.0\lib\net48\FlaUI.Core.dll"
Add-Type -Path "$pkgRoot\FlaUI.UIA3.4.0.0\lib\net48\FlaUI.UIA3.dll"

$proc = Get-Process -Name "HeroVirtualDesktop" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Host "App not running"; exit 1 }

$automation = New-Object FlaUI.UIA3.UIA3Automation
$app = [FlaUI.Core.Application]::Attach($proc)
$win = $app.GetMainWindow($automation, [TimeSpan]::FromSeconds(15))
Write-Host "Window: $($win.Name)"

# Search for btBrowse
$cf = $win.ConditionFactory
$btn = $win.FindFirstDescendant($cf.ByAutomationId("btBrowse"))
if ($btn) {
    Write-Host "FOUND btBrowse: Name=$($btn.Name) Visible=$(-not $btn.IsOffscreen)"
} else {
    Write-Host "btBrowse NOT FOUND - searching all buttons..."
    $buttons = $win.FindAllDescendants($cf.ByControlType([FlaUI.Core.Definitions.ControlType]::Button))
    Write-Host "Total buttons found: $($buttons.Count)"
    $buttons | Select-Object -First 20 | ForEach-Object {
        Write-Host "  Button aid='$($_.AutomationId)' name='$($_.Name)' tooltip='$($_.HelpText)'"
    }
}

# Also find the tree
$tree = $win.FindFirstDescendant($cf.ByAutomationId("treeViewCrowd"))
if ($tree) {
    Write-Host "treeViewCrowd FOUND"
    $items = $tree.FindAllChildren($cf.ByControlType([FlaUI.Core.Definitions.ControlType]::TreeItem))
    Write-Host "  Top-level tree items: $($items.Count)"
    $items | ForEach-Object { Write-Host "    - $($_.Name)" }
} else {
    Write-Host "treeViewCrowd NOT FOUND"
}

$automation.Dispose()
