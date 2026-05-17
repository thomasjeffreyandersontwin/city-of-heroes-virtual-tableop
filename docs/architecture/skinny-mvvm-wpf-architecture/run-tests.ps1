$root   = "c:\hero-desktop\city-of-heroes-virtual-tabletop"
$runner = "c:\hero-desktop\test-runner\runner.exe"
$dll    = "$root\HerovirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.UnitTest\bin\Debug\Module.UnitTest.dll"

Set-Location $root

& $runner $dll "ExampleCharacterDomainTests"
& $runner $dll "ExampleRosterViewModelTests"
