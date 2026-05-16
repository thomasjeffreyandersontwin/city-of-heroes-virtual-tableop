$root = Split-Path $PSScriptRoot -Parent
$wpf = "$root\HerovirtualTableTop\HeroVirtualTabletop.WPF"
$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$slnDir = "$wpf\"
$runner = "c:\hero-desktop\test-runner\runner.exe"
$testDll = "$wpf\Modules\Module.UnitTest\bin\Debug\Module.UnitTest.dll"
$results = "c:\hero-desktop\test-results-full.txt"

Write-Host "=== Building tests ===" -ForegroundColor Cyan
& $msbuild "$wpf\Modules\Module.UnitTest\Module.UnitTest.csproj" /p:Configuration=Debug "/p:SolutionDir=$slnDir" /nologo /verbosity:quiet 2>&1 | Select-String "error CS"
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED" -ForegroundColor Red; exit 1 }
Write-Host "=== Build OK ===" -ForegroundColor Green

Write-Host "=== Running tests ===" -ForegroundColor Cyan
& $runner $testDll 2>&1 | Tee-Object -FilePath $results

$content = Get-Content $results
$passed = ($content | Select-String "^\s+PASS:" | Measure-Object).Count
$failed = ($content | Select-String "^\s+FAIL:" | Measure-Object).Count
Write-Host ""
Write-Host "==============================" -ForegroundColor Cyan
Write-Host "PASSED: $passed / $($passed+$failed)" -ForegroundColor Green
Write-Host "FAILED: $failed / $($passed+$failed)" -ForegroundColor $(if($failed -gt 0){"Red"}else{"Green"})
Write-Host "==============================" -ForegroundColor Cyan
Write-Host "Full results: $results"
