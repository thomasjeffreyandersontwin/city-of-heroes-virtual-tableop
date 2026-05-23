# Dump the UIA tree of a running Hero VTT window
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$desktop = [System.Windows.Automation.AutomationElement]::RootElement
$exe = "HeroVirtualDesktop"

# Find the app window
$wins = $desktop.FindAll([System.Windows.Automation.TreeScope]::Children,
    [System.Windows.Automation.Condition]::TrueCondition)
$appWin = $null
foreach ($w in $wins) {
    try {
        $pn = (Get-Process -Id $w.Current.ProcessId -ErrorAction SilentlyContinue).ProcessName
        if ($pn -like "*Hero*" -or $pn -like "*Virtual*") {
            $appWin = $w
            Write-Host "Found: '$($w.Current.Name)' proc=$pn"
            break
        }
    } catch {}
}

if (-not $appWin) {
    Write-Host "No Hero window found. Running processes:"
    Get-Process | Where-Object { $_.MainWindowTitle -ne "" } | Select-Object Id, ProcessName, MainWindowTitle
    exit
}

function Dump-Tree($el, $depth) {
    $indent = "  " * $depth
    $ct = $el.Current.ControlType.ProgrammaticName
    $n  = $el.Current.Name
    $id = $el.Current.AutomationId
    Write-Host "${indent}[$ct] '$n' id='$id'"

    if ($depth -ge 6) { return }
    $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $child = $walker.GetFirstChild($el)
    while ($child) {
        Dump-Tree $child ($depth + 1)
        $child = $walker.GetNextSibling($child)
    }
}

Dump-Tree $appWin 0
