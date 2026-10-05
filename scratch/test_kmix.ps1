$p = Start-Process -FilePath "Kashtrix-Mix\src\KMix.App\bin\x64\Debug\net8.0-windows\win-x64\K-Mix.exe" -PassThru
Start-Sleep -Seconds 3
$p.Refresh()
if (-not $p.HasExited) {
    Write-Host "SUCCESS: K-Mix.exe is running without crashing! PID: $($p.Id)"
    $p.Kill()
} else {
    Write-Host "FAILED: K-Mix.exe exited with code $($p.ExitCode)"
}
