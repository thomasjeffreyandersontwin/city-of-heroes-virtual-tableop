param()

$base        = "c:\hero-desktop\city-of-heroes-virtual-tabletop\tests\e2e"
$runFlag     = "$base\agent2-run.flag"
$doneFlag    = "$base\agent2-done.flag"
$resultsFile = "$base\agent2-results.txt"
$allDoneFlag = "$base\agent1-all-done.flag"

$msbuild     = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
$csproj      = "$base\CrowdManagement.E2ETests.csproj"
$dll         = "$base\bin\Debug\CrowdManagement.E2ETests.dll"
$runner      = "c:\hero-desktop\city-of-heroes-virtual-tabletop\test-runner\e2e-runner.exe"
$errorLog    = "C:\hero-desktop\city-of-heroes-virtual-tabletop\data\crowd-load-error.log"

$lockFile = "$base\agent2-loop.pid"

function Write-Log {
    param([string]$msg)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $msg"
}

# Single-instance guard: lowest PID wins (avoids race when two start simultaneously)
$allInstances = @(Get-WmiObject Win32_Process | Where-Object {
    $_.CommandLine -like "*agent2-loop.ps1*"
})
$lowestPid = ($allInstances | Measure-Object -Property ProcessId -Minimum).Minimum
if ($lowestPid -ne $PID) {
    Write-Log "Yielding to lower-PID instance ($lowestPid). Exiting."
    exit 0
}
$PID | Set-Content $lockFile -Encoding UTF8

function Build-TestDll {
    Write-Log "Building E2E test DLL..."
    $result = & $msbuild $csproj /p:Configuration=Debug /verbosity:minimal 2>&1
    $exitCode = $LASTEXITCODE
    Write-Log "MSBuild exit code: $exitCode"
    $result | ForEach-Object { Write-Host "  $_" }
    return ($exitCode -eq 0)
}

function Run-Tests {
    $failures = @()

    # Build (or rebuild to pick up new test files from Agent 1)
    $built = Build-TestDll
    if (-not $built) {
        $failures += "BUILD FAILED: Could not compile CrowdManagement.E2ETests.dll"
        return ,$failures
    }
    if (-not (Test-Path $dll)) {
        $failures += "BUILD FAILED: DLL still missing after build attempt"
        return ,$failures
    }

    # Verify runner.exe is present
    if (-not (Test-Path $runner)) {
        $failures += "SETUP ERROR: runner.exe not found at $runner"
        return ,$failures
    }
    Write-Log "Using runner: $runner"

    # Run tests — write runner output to a file so that HeroVirtualDesktop.exe
    # inherits a file handle (not a pipe). Pipe buffers are small; if the app
    # writes DEBUG logs and the buffer fills, the child process blocks and
    # appears to exit unexpectedly. A file handle never blocks on write.
    $runnerOut = "$base\runner-output.txt"
    Write-Log "Running E2E tests..."
    cmd /c """$runner"" ""$dll"" > ""$runnerOut"" 2>&1"
    $runnerExit = $LASTEXITCODE
    Write-Log "runner exit code: $runnerExit"
    $testOutput = if (Test-Path $runnerOut) { Get-Content $runnerOut } else { @() }
    $testOutput | ForEach-Object { Write-Host "  $_" }

    # Parse FAIL: lines (format: "  FAIL: ClassName.MethodName (Xms)")
    # followed by "        reason" on the next line
    $lines = @($testOutput)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^\s+FAIL:\s+(.+)$") {
            $label = $Matches[1].Trim()
            $reason = ""
            if (($i + 1) -lt $lines.Count -and $lines[$i+1] -match "^\s{8}(.+)$") {
                $reason = $Matches[1].Trim()
                $i++
            }
            if ($reason) {
                $failures += "FAILED: $label - $reason"
            } else {
                $failures += "FAILED: $label"
            }
        }
    }

    # Fallback: runner exited non-zero but nothing parsed
    if ($failures.Count -eq 0 -and $runnerExit -ne 0) {
        $failLines = $testOutput | Where-Object { $_ -match "FAIL" }
        if ($failLines) {
            $failures += "TEST FAILURES (raw): " + ($failLines -join " | ")
        } else {
            # Include full runner output so Agent 1 can see errors like file locks
            $allOutput = ($testOutput | Where-Object { $_ -and $_.Trim() }) -join " | "
            if ($allOutput) {
                $failures += "RUNNER EXITED CODE $runnerExit : $allOutput"
            } else {
                $failures += "RUNNER EXITED WITH CODE $runnerExit (no output)"
            }
        }
    }

    # Append crash log if non-empty
    if (Test-Path $errorLog) {
        $crashContent = Get-Content $errorLog -Raw -ErrorAction SilentlyContinue
        if ($crashContent -and $crashContent.Trim()) {
            $failures += "APP ERROR LOG: $crashContent"
        }
    }

    return ,$failures
}

# ============================================================
# Main polling loop
# ============================================================
Write-Log "Agent 2 online. Polling for agent2-run.flag every 8 s..."
Write-Log "  run-flag : $runFlag"
Write-Log "  all-done : $allDoneFlag"
Write-Log "  runner   : $runner"

while (-not (Test-Path $allDoneFlag)) {
    if (Test-Path $runFlag) {
        Write-Log "Run flag detected - starting test cycle"
        Remove-Item $doneFlag -Force -ErrorAction SilentlyContinue

        $failures = @()
        try {
            $failures = Run-Tests
        } catch {
            $failures = @("UNEXPECTED ERROR in Agent 2 loop: $_")
            Write-Log "ERROR: $_"
        } finally {
            Write-Log "Killing HeroVirtualDesktop.exe..."
            taskkill /f /im HeroVirtualDesktop.exe 2>$null | Out-Null
            Start-Sleep -Seconds 2
        }

        if ($failures.Count -eq 0) {
            Write-Log "All tests PASSED - writing empty results file"
            "" | Set-Content $resultsFile -Encoding UTF8
        } else {
            Write-Log "Failures found ($($failures.Count)) - writing results"
            $failures | Set-Content $resultsFile -Encoding UTF8
            $failures | ForEach-Object { Write-Log "  >> $_" }
        }

        Write-Log "Writing agent2-done.flag..."
        "done" | Set-Content $doneFlag -Encoding UTF8

        Remove-Item $runFlag -Force -ErrorAction SilentlyContinue
        Write-Log "Cycle complete. Resuming poll..."
    }

    Start-Sleep -Seconds 8
}

Write-Log "agent1-all-done.flag detected. Agent 2 exiting cleanly."
Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
