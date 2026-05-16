$targets = @("HeroVirtualDesktop", "cityofheroes", "Tequila", "CreamSoda")

foreach ($name in $targets) {
    $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($procs) {
        $procs | Stop-Process -Force
        Write-Host "Killed $name ($($procs.Count) process(es))" -ForegroundColor Yellow
    }
}

Write-Host "=== All Hero/CoH processes stopped ===" -ForegroundColor Green
