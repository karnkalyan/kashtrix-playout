$json = Get-Content 'scratch_ytdlp.json' -Raw | ConvertFrom-Json
Write-Host "Formats count: $($json.requested_formats.Count)"
foreach ($f in $json.requested_formats) {
    Write-Host "format_id: $($f.format_id) vcodec: $($f.vcodec) acodec: $($f.acodec) ext: $($f.ext)"
    Write-Host "url: $($f.url.Substring(0, 80))"
}
