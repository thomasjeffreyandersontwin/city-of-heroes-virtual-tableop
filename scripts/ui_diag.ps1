$pkgRoot = "C:\hero-desktop\city-of-heroes-virtual-tabletop\HerovirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.UITest\packages"
Add-Type -Path "$pkgRoot\Interop.UIAutomationClient.10.19041.0\lib\net45\Interop.UIAutomationClient.dll"
Add-Type -Path "$pkgRoot\FlaUI.Core.4.0.0\lib\net48\FlaUI.Core.dll"
Add-Type -Path "$pkgRoot\FlaUI.UIA3.4.0.0\lib\net48\FlaUI.UIA3.dll"

$automation = New-Object FlaUI.UIA3.UIA3Automation
$proc = Get-Process -Name "HeroVirtualDesktop" -ErrorAction SilentlyContinue | Select-Object -First 1
$app = [FlaUI.Core.Application]::Attach($proc)
$mainWin = $app.GetMainWindow($automation, [TimeSpan]::FromSeconds(10))
Write-Host "Main window AutomationId: $($mainWin.AutomationId)"

function Dump-Tree {
    param($el, $depth = 0)
    $indent = "  " * $depth
    $aid = $el.AutomationId
    $name = $el.Name
    $ct = $el.ControlType
    $ht = ""
    try { $ht = $el.HelpText } catch {}
    Write-Host "${indent}[$ct] AutomationId='$aid' Name='$name' HelpText='$ht'"
    if ($depth -lt 6) {
        $trueC = [FlaUI.Core.Conditions.TrueCondition]::Default
        $kids = $el.FindAllChildren($trueC)
        foreach ($k in $kids) { Dump-Tree $k ($depth+1) }
    }
}
Dump-Tree $mainWin
$automation.Dispose()
