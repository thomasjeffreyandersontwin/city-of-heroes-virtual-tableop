# UI automation test: launch Hero VTT, click Browse, load Armageddons.data,
# verify Pre-Emptive Strike / Spyder / Suzerain appear in the tree.
#
# Uses .NET UIAutomationClient (built-in to Windows).

param([switch]$NoLaunch)  # pass -NoLaunch to attach to already-running app

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class WinClick {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint c, int e);
    public static void Click(double px, double py, double w, double h) {
        int cx = (int)(px + w / 2);
        int cy = (int)(py + h / 2);
        SetCursorPos(cx, cy);
        mouse_event(2, 0, 0, 0, 0);
        mouse_event(4, 0, 0, 0, 0);
    }
}
'@ -ErrorAction SilentlyContinue

$root     = Split-Path $PSScriptRoot
$exe      = Join-Path $root "HerovirtualTableTop\HeroVirtualTabletop.WPF\Shell\HeroVirtualTableTop.Shell\bin\Debug\HeroVirtualDesktop.exe"
$dataFile = Join-Path $root "data\crowds\rebuilt\Armageddons.data"

Write-Host "EXE : $exe"
Write-Host "File: $dataFile"

# ── helpers ───────────────────────────────────────────────────────────────────

function uia-FindAll($el, $ctrlType) {
    $prop = [System.Windows.Automation.AutomationElement]::ControlTypeProperty
    $cond = New-Object System.Windows.Automation.PropertyCondition($prop, $ctrlType)
    return $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function uia-FindById($el, $automationId) {
    $prop = [System.Windows.Automation.AutomationElement]::AutomationIdProperty
    $cond = New-Object System.Windows.Automation.PropertyCondition($prop, $automationId)
    return $el.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function uia-Invoke($el) {
    # Try TogglePattern (WPF Expander header buttons)
    try {
        $p = $el.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
        $p.Toggle()
        return $true
    } catch { }
    # Try InvokePattern
    try {
        $p = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $p.Invoke()
        return $true
    } catch { }
    # Try ExpandCollapsePattern
    try {
        $p = $el.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
        $p.Expand()
        return $true
    } catch { }
    # Fallback: raw mouse click
    try {
        $r = $el.Current.BoundingRectangle
        [WinClick]::Click($r.X, $r.Y, $r.Width, $r.Height)
    } catch { }
    return $true
}

# ── launch / attach ───────────────────────────────────────────────────────────

$desktop = [System.Windows.Automation.AutomationElement]::RootElement

if ($NoLaunch) {
    Write-Host "Attaching to running Hero VTT..."
    $appProc = Get-Process HeroVirtualDesktop -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $appProc) { Write-Host "FAIL: not running"; exit 1 }
} else {
    Write-Host "Launching app..."
    $appProc = Start-Process -FilePath $exe -WorkingDirectory $root -PassThru
}

$appPid = $appProc.Id
Write-Host "PID: $appPid"

# Wait for main window
Write-Host "Waiting for window..."
$appWin = $null
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $pidCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $appPid)
    $w = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Children, $pidCond)
    if ($w -and $w.Current.Name -ne "") { $appWin = $w; break }
}
if (-not $appWin) { Write-Host "FAIL: window not found"; exit 1 }
Write-Host "Window: '$($appWin.Current.Name)'"

# Wait for loading indicator to clear
Write-Host "Waiting for app ready..."
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $loadEl = uia-FindById $appWin "tbLoadingText"
    if (-not $loadEl -or $loadEl.Current.Name -eq "") { Write-Host "  Ready."; break }
    Write-Host "  Loading ($i)..."
}

# ── expand Character Explorer ────────────────────────────────────────────────

Write-Host "Expanding Character Explorer..."
$btnCT  = [System.Windows.Automation.ControlType]::Button
$allBtns = uia-FindAll $appWin $btnCT
$expBtn  = $null
foreach ($b in $allBtns) {
    if ($b.Current.Name -eq "Character Explorer") { $expBtn = $b; break }
}
if ($expBtn) {
    uia-Invoke $expBtn | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "  Clicked expander."
} else {
    Write-Host "  Character Explorer button not found — listing buttons:"
    foreach ($b in $allBtns) {
        Write-Host "    '$($b.Current.Name)' id='$($b.Current.AutomationId)'"
    }
}

# ── find Browse button ────────────────────────────────────────────────────────

Write-Host "Looking for Browse button (AutomationId=btBrowse)..."
$browseBtn = $null
for ($i = 0; $i -lt 10; $i++) {
    $browseBtn = uia-FindById $appWin "btBrowse"
    if ($browseBtn) { break }
    Start-Sleep -Milliseconds 500
}

if (-not $browseBtn) {
    Write-Host "  Not found by id. All buttons after expansion:"
    $allBtns2 = uia-FindAll $appWin $btnCT
    foreach ($b in $allBtns2) {
        Write-Host "    '$($b.Current.Name)' id='$($b.Current.AutomationId)' help='$($b.Current.HelpText)'"
    }
    Write-Host "FAIL: Browse button not found"
    if (-not $NoLaunch) { $appProc.Kill() }
    exit 1
}

Write-Host "  Found Browse button."

# ── click Browse and handle file dialog ──────────────────────────────────────

Write-Host "Clicking Browse..."
uia-Invoke $browseBtn | Out-Null
Start-Sleep -Seconds 1

Write-Host "Waiting for file dialog..."
$fileDlg = $null
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Milliseconds 500
    $allDesktopWins = $desktop.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($dw in $allDesktopWins) {
        try {
            if ($dw.Current.ProcessId -ne $appPid) { continue }
            $dwName = $dw.Current.Name
            if ($dwName -ne "MainWindowV2" -and $dwName -ne "") {
                Write-Host "  Found child window: '$dwName'"
                $fileDlg = $dw
                break
            }
        } catch { }
    }
    if ($fileDlg) { break }
}

if ($fileDlg) {
    Write-Host "Filling dialog: '$($fileDlg.Current.Name)'"
    $editCT = [System.Windows.Automation.ControlType]::Edit
    $edits  = uia-FindAll $fileDlg $editCT
    if ($edits.Count -gt 0) {
        try {
            $vp = $edits[0].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
            $vp.SetValue($dataFile)
        } catch {
            $edits[0].SetFocus()
            [System.Windows.Forms.SendKeys]::SendWait("^a")
            [System.Windows.Forms.SendKeys]::SendWait($dataFile)
        }
    }
} else {
    Write-Host "  No dialog via UIA — sending keys"
    [System.Windows.Forms.SendKeys]::SendWait($dataFile)
}

Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Start-Sleep -Seconds 4

# ── read tree items ───────────────────────────────────────────────────────────

Write-Host "Reading tree items..."
$treeItemCT = [System.Windows.Automation.ControlType]::TreeItem
$treeItems  = uia-FindAll $appWin $treeItemCT
$names = @()
foreach ($ti in $treeItems) {
    $n = $ti.Current.Name
    if ($n) { $names += $n }
}
Write-Host "Tree: $($names -join ' | ')"

# ── verdict ───────────────────────────────────────────────────────────────────

$expected = @("Pre-Emptive Strike", "Spyder", "Suzerain")
$missing  = @()
foreach ($e in $expected) {
    if ($names -notcontains $e) { $missing += $e }
}

if (-not $NoLaunch) { $appProc.Kill() }

if ($missing.Count -gt 0) {
    Write-Host "FAIL: missing from tree: $($missing -join ', ')"
    exit 1
} else {
    Write-Host "PASS: all 3 Armageddon characters loaded."
    exit 0
}
