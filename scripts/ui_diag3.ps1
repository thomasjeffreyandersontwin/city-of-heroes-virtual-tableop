$pkgRoot = "C:\hero-desktop\city-of-heroes-virtual-tabletop\HerovirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.UITest\packages"
Add-Type -Path "$pkgRoot\Interop.UIAutomationClient.10.19041.0\lib\net45\Interop.UIAutomationClient.dll"
Add-Type -Path "$pkgRoot\FlaUI.Core.4.0.0\lib\net48\FlaUI.Core.dll"
Add-Type -Path "$pkgRoot\FlaUI.UIA3.4.0.0\lib\net48\FlaUI.UIA3.dll"

$automation = New-Object FlaUI.UIA3.UIA3Automation
$proc = Get-Process -Name "HeroVirtualDesktop" -ErrorAction SilentlyContinue | Select-Object -First 1
$app = [FlaUI.Core.Application]::Attach($proc)
$mainWin = $app.GetMainWindow($automation, [TimeSpan]::FromSeconds(10))

$cf = $mainWin.ConditionFactory
$ceGroup = $mainWin.FindFirstDescendant(
    $cf.ByControlType([FlaUI.Core.Definitions.ControlType]::Group).And($cf.ByName("Character Explorer")))
if ($ceGroup) {
    Write-Host "Found Character Explorer group"
    $expanderBtn = $ceGroup.FindFirstDescendant($cf.ByAutomationId("ExpanderButton"))
    if ($expanderBtn) {
        Write-Host "Button patterns: $(($expanderBtn.Patterns | Get-Member -MemberType Property | ForEach-Object { $_.Name }) -join ', ')"
        # Try Invoke pattern
        $inv = $expanderBtn.Patterns.Invoke.PatternOrDefault
        if ($inv) {
            Write-Host "Using Invoke pattern..."
            $inv.Invoke()
        } else {
            Write-Host "No Invoke - trying Toggle..."
            $tog = $expanderBtn.Patterns.Toggle.PatternOrDefault
            if ($tog) { $tog.Toggle() }
            else {
                Write-Host "No Toggle - using Click()"
                $expanderBtn.Click()
            }
        }
        Start-Sleep -Seconds 2
        Write-Host "Done expanding."
    }
}

function Dump-Tree {
    param($el, $depth = 0)
    $indent = "  " * $depth
    $aid = $el.AutomationId
    $name = $el.Name
    $ct = $el.ControlType
    $ht = ""
    try { $ht = $el.HelpText } catch {}
    Write-Host "${indent}[$ct] AutomationId='$aid' Name='$name' HelpText='$ht'"
    if ($depth -lt 8) {
        $trueC = [FlaUI.Core.Conditions.TrueCondition]::Default
        $kids = $el.FindAllChildren($trueC)
        foreach ($k in $kids) { Dump-Tree $k ($depth+1) }
    }
}
Write-Host "--- Full tree after expand ---"
Dump-Tree $mainWin
$automation.Dispose()
