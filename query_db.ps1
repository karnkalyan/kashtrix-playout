$dll = (Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'KashtrixPlayout') -Filter 'Microsoft.Data.Sqlite.dll' -Recurse | Select-Object -First 1).FullName
Add-Type -Path $dll

$dbPath = Join-Path $env:LOCALAPPDATA 'KashtrixPlayout\kashtrix.db'
$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath;Mode=ReadOnly")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT seq, event_type, title, file_path, source_kind, item_json FROM playlist ORDER BY seq"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "Seq: $($reader.GetInt32(0)) | Type: $($reader.GetString(1)) | Title: $($reader.GetString(2)) | SourceKind: $($reader.GetString(4))"
    Write-Host "FilePath: $($reader.GetString(3).Substring(0, [Math]::Min(80, $reader.GetString(3).Length)))"
    $json = $reader.GetString(5)
    if ($json -match '"AlternateAudioUrl":"([^"]+)"') {
        Write-Host "AlternateAudioUrl in JSON: $($Matches[1].Substring(0, [Math]::Min(80, $Matches[1].Length)))"
    } else {
        Write-Host "AlternateAudioUrl in JSON: NONE"
    }
    Write-Host "---"
}
$conn.Close()
