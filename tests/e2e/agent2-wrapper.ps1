param()
$loop   = "c:\hero-desktop\city-of-heroes-virtual-tabletop\tests\e2e\agent2-loop.ps1"
$allDone = "c:\hero-desktop\city-of-heroes-virtual-tabletop\tests\e2e\agent1-all-done.flag"
while (-not (Test-Path $allDone)) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Wrapper: starting inner loop..."
    & powershell -NonInteractive -ExecutionPolicy Bypass -File $loop
    $ec = $LASTEXITCODE
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Wrapper: inner loop exited (code $ec)"
    if (Test-Path $allDone) { break }
    Remove-Item "c:\hero-desktop\city-of-heroes-virtual-tabletop\tests\e2e\agent2-loop.pid" -Force -ErrorAction SilentlyContinue
    taskkill /f /im HeroVirtualDesktop.exe 2>$null | Out-Null
    Start-Sleep -Seconds 5
}
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Wrapper: agent1-all-done.flag seen. Done."
