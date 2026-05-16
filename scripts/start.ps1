$root = Split-Path $PSScriptRoot -Parent
$wpf = "$root\HerovirtualTableTop\HeroVirtualTabletop.WPF"
$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$slnDir = "$wpf\"

Write-Host "=== Restoring NuGet packages ===" -ForegroundColor Cyan
& "$wpf\.nuget\NuGet.exe" restore "$wpf\HeroVirtualTabletop.WPF.sln" | Out-Null

Write-Host "=== Building ===" -ForegroundColor Cyan
& $msbuild "$wpf\Shell\HeroVirtualTableTop.Shell\HeroVirtualTabletop.Shell.csproj" `
    /p:Configuration=Debug "/p:SolutionDir=$slnDir" /nologo /verbosity:quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "=== Build OK ===" -ForegroundColor Green

$exe = "$wpf\Shell\HeroVirtualTableTop.Shell\bin\Debug\HeroVirtualDesktop.exe"
Write-Host "=== Launching HeroVirtualDesktop ===" -ForegroundColor Cyan
Start-Process $exe -WorkingDirectory (Split-Path $exe)
Write-Host "=== Running ===" -ForegroundColor Green
