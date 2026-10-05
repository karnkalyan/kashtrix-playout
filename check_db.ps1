$db = Join-Path $env:LOCALAPPDATA 'KashtrixPlayout\kashtrix.db'
if (Test-Path $db) {
    Write-Host "DB exists at $db"
} else {
    Write-Host "DB not found at $db"
}
