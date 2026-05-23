<#
.SYNOPSIS
  FlaUI test: attach to running HeroVirtualDesktop, verify Armageddons crowd
  shows 3 sub-crowds in the tree. Run this AFTER the app is already up.
.USAGE
  .\scripts\ui_test_crowd_load.ps1
#>
Set-StrictMode -Off
$ErrorActionPreference = "Continue"

$pkgRoot = "C:\hero-desktop\city-of-heroes-virtual-tabletop\HerovirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.UITest\packages"
Add-Type -Path "$pkgRoot\Interop.UIAutomationClient.10.19041.0\lib\net45\Interop.UIAutomationClient.dll"
Add-Type -Path "$pkgRoot\FlaUI.Core.4.0.0\lib\net48\FlaUI.Core.dll"
Add-Type -Path "$pkgRoot\FlaUI.UIA3.4.0.0\lib\net48\FlaUI.UIA3.dll"
Write-Host "FlaUI loaded."

$passed = 0; $failed = 0
function Pass($msg) { Write-Host "  PASS: $msg" -ForegroundColor Green;  $script:passed++ }
function Fail($msg) { Write-Host "  FAIL: $msg" -ForegroundColor Red;    $script:failed++ }

# Attach to app
$proc = Get-Process -Name "HeroVirtualDesktop" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Host "App not running"; exit 1 }
Write-Host "Attaching to PID $($proc.Id)..."

$automation = New-Object FlaUI.UIA3.UIA3Automation
$app = [FlaUI.Core.Application]::Attach($proc)
$win = $app.GetMainWindow($automation, [TimeSpan]::FromSeconds(20))
if (-not $win) { Write-Host "Main window not found"; exit 1 }
Write-Host "Window: $($win.Name)"

$cf = $win.ConditionFactory

# Ensure Character Explorer section is expanded (btBrowse only exists when expanded)
$tree = $win.FindFirstDescendant($cf.ByAutomationId("treeViewCrowd"))
if (-not $tree) {
    Write-Host "Character Explorer collapsed — expanding..."
    $expandBtn = $win.FindFirstDescendant(
        $cf.ByName("Character Explorer").And($cf.ByControlType([FlaUI.Core.Definitions.ControlType]::Button)))
    if ($expandBtn) {
        $expandBtn.Click()
        Start-Sleep -Seconds 1
        $tree = $win.FindFirstDescendant($cf.ByAutomationId("treeViewCrowd"))
    }
}

# TEST 1: Browse button
Write-Host "`nTEST 1: Browse button visible"
$btn = $win.FindFirstDescendant($cf.ByAutomationId("btBrowse"))
if (-not $btn) {
    $btn = $win.FindFirstDescendant($cf.ByHelpText("Browse Crowd Files..."))
}
if ($btn) { Pass "Browse button found (aid=$($btn.AutomationId))" }
else      { Fail "Browse button not found" }

# TEST 2: Armageddons tree node exists
# Tree items expose crowd name via textBlockCrowd Edit child (Value pattern), not via Name property
Write-Host "`nTEST 2: Armageddons crowd in tree"
$arma = $null
if (-not $tree) { Fail "treeViewCrowd not found" }
else {
    $topItems = $tree.FindAllChildren($cf.ByControlType([FlaUI.Core.Definitions.ControlType]::TreeItem))
    foreach ($item in $topItems) {
        $edit = $item.FindFirstDescendant($cf.ByAutomationId("textBlockCrowd"))
        if ($edit) {
            $vp = $edit.Patterns.Value.PatternOrDefault
            if ($vp -and ([string]$vp.Value).Trim() -eq "Armageddons") {
                $arma = $item
                break
            }
        }
    }
    if ($arma) { Pass "Armageddons node in tree" }
    else       { Fail "Armageddons node NOT in tree" }
}

# TEST 3: 3 sub-crowds (crowd members)
# Member items expose their name via textBlockCharacter Edit child (Value pattern)
Write-Host "`nTEST 3: 3 sub-crowds under Armageddons"
if ($arma) {
    $expand = $arma.Patterns.ExpandCollapse.PatternOrDefault
    if ($expand) { $expand.Expand(); Start-Sleep -Milliseconds 800 }

    $kids = $arma.FindAllChildren($cf.ByControlType([FlaUI.Core.Definitions.ControlType]::TreeItem))
    $names = @()
    foreach ($kid in $kids) {
        $edit = $kid.FindFirstDescendant($cf.ByAutomationId("textBlockCharacter"))
        if ($edit) {
            $vp = $edit.Patterns.Value.PatternOrDefault
            if ($vp) { $names += ([string]$vp.Value).Trim() }
        } elseif ($kid.Name -and $kid.Name -notlike "Module.*") {
            $names += $kid.Name
        }
    }
    Write-Host "  Children: $($names -join ', ')"

    if ($names.Count -ge 3) { Pass "$($names.Count) sub-crowds loaded" }
    else                    { Fail "Expected 3 sub-crowds, got $($names.Count)" }

    foreach ($expected in @("Pre-Emptive Strike","Spyder","Suzerain")) {
        if ($names -contains $expected) { Pass "'$expected' present" }
        else                            { Fail "'$expected' NOT found" }
    }
} else {
    Fail "Skipped - Armageddons node not found"
    Fail "Skipped - sub-crowd count"
    Fail "Skipped - Pre-Emptive Strike"
    Fail "Skipped - Spyder"
    Fail "Skipped - Suzerain"
}

$automation.Dispose()

Write-Host "`n================================"
Write-Host "PASSED: $passed  FAILED: $failed"
Write-Host "================================"
exit $(if ($failed -gt 0) { 1 } else { 0 })
