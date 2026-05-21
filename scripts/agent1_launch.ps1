# Agent 1: set crowd, kill old app, build+launch, signal ready
Set-StrictMode -Off
$root = "C:\hero-desktop\city-of-heroes-virtual-tabletop"
Set-Location $root

# Signal not ready yet
Remove-Item "$root\app-ready.flag" -ErrorAction SilentlyContinue
Remove-Item "$root\crowd-load-error.log" -ErrorAction SilentlyContinue

# Point at Armageddons.data
$path = "$root/data/crowds/Armageddons.data".Replace("\","/")
python -c "import json; open('data/active-crowds.json','w').write(json.dumps(['$path'])); print('active-crowds.json set')"

# Kill old instance
taskkill /f /im HeroVirtualDesktop.exe 2>$null
Start-Sleep -Seconds 2

# Build + launch
Write-Host "=== Building and launching ==="
& powershell -ExecutionPolicy Bypass -File "$root\scripts\start.ps1"
Write-Host "start.ps1 done, waiting 20s for app to load..."
Start-Sleep -Seconds 20

# Check result
$proc = Get-Process -Name "HeroVirtualDesktop" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) {
    Write-Host "AGENT1 FAIL: App not running after launch"
    exit 1
}
Write-Host "App running PID $($proc.Id)"

if (Test-Path "$root\crowd-load-error.log") {
    Write-Host "AGENT1 FAIL: Crowd load errors:"
    Get-Content "$root\crowd-load-error.log"
    exit 1
}

# Signal ready
Set-Content "$root\app-ready.flag" $proc.Id
Write-Host "AGENT1 DONE: app-ready.flag written"
