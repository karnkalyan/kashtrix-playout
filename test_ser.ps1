$code = @'
using System;
using System.Text.Json;
using BroadcastPlayout.Models;

var item = new PlaylistItem {
    Title = "Live Input",
    FilePath = "https://video.url",
    AlternateAudioUrl = "https://audio.url",
    SourceKind = "URL"
};

var json = JsonSerializer.Serialize(item);
Console.WriteLine("JSON: " + json);
var deserialized = JsonSerializer.Deserialize<PlaylistItem>(json);
Console.WriteLine("Deserialized AlternateAudioUrl: " + deserialized.AlternateAudioUrl);
'@
Out-File -FilePath "test_ser.cs" -InputObject $code -Encoding utf8
