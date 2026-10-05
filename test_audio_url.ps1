$json = Get-Content 'scratch_ytdlp.json' -Raw | ConvertFrom-Json
$audioUrl = $json.requested_formats[1].url
$ffmpeg = ".\src\BroadcastPlayout.App\native\ffmpeg\ffmpeg.exe"
& $ffmpeg -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -timeout 5000000 -i $audioUrl 2>&1 | Select-Object -First 30
