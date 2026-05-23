# Check tree items - deep dump including text elements inside tree items
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$desktop = [System.Windows.Automation.AutomationElement]::RootElement
$nc = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "MainWindowV2")
$appWin = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Children, $nc)
if (-not $appWin) { Write-Host "no window"; exit }

# Make sure Character Explorer is expanded - toggle its button
$btnCT  = [System.Windows.Automation.ControlType]::Button
$btnC   = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $btnCT)
$nameC  = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Character Explorer")
$andC   = New-Object System.Windows.Automation.AndCondition($btnC, $nameC)
$expBtn = $appWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $andC)

# Check if already expanded by looking for btBrowse
$idC       = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "btBrowse")
$isExpanded = $appWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $idC)
if (-not $isExpanded) {
    if ($expBtn) {
        try {
            $tp = $expBtn.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
            $tp.Toggle()
            Write-Host "Toggled expander"
            Start-Sleep -Seconds 2
        } catch { Write-Host "Toggle failed" }
    }
}

# Deep tree dump - text and tree items
Write-Host "--- Deep tree dump (up to depth 10) ---"
function DumpDeep($el, $depth) {
    if ($depth -gt 10) { return }
    $indent = "  " * $depth
    $ct = $el.Current.ControlType.ProgrammaticName -replace "ControlType\.",""
    $n  = $el.Current.Name
    $id = $el.Current.AutomationId
    if ($n -or $id) {
        Write-Host "${indent}[$ct] '$n' id='$id'"
    }
    if ($ct -eq "Text" -or $ct -eq "TreeItem") {
        # show all text children
    }
    $w = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $c = $w.GetFirstChild($el)
    while ($c) {
        DumpDeep $c ($depth + 1)
        $c = $w.GetNextSibling($c)
    }
}
DumpDeep $appWin 0
