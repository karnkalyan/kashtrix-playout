using FFmpeg.AutoGen.Abstractions;
using BroadcastPlayout.Models;
using BroadcastPlayout.Outputs;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Ffmpeg;

public sealed unsafe class AudioDecoder : IDisposable
{
    private AVFormatContext* _format;
    private readonly AVCodecContext* _codec;
    private readonly AVFrame* _frame;
    private readonly AVPacket* _packet;
    private SwrContext* _swr;
    private readonly int _streamIndex;
    private readonly AVRational _timeBase;
    private bool _inputEof;
    private bool _flushSent;
    private const int OutRate = 48000;
    private readonly int _outChannels;
    private readonly PlaylistItem _item;
    private readonly SharedAudioBusReader? _sharedAudioReader;

    public AudioDecoder(string file) : this(new PlaylistItem { FilePath = file, SourceKind = "File" }) { }

    public AudioDecoder(PlaylistItem item)
    {
        _item = item;

        if (item.SourceKind.Equals("SharedMemory", StringComparison.OrdinalIgnoreCase) ||
            item.SourceKind.Equals("KashtrixStudio", StringComparison.OrdinalIgnoreCase) ||
            item.FilePath.StartsWith("KashtrixStudio.", StringComparison.OrdinalIgnoreCase))
        {
            var audioMap = !string.IsNullOrWhiteSpace(item.AlternateAudioUrl) && item.AlternateAudioUrl.StartsWith("Kashtrix", StringComparison.OrdinalIgnoreCase)
                ? item.AlternateAudioUrl
                : VirtualOutputBridge.StudioProgramAudioMapName;
            _sharedAudioReader = new SharedAudioBusReader(audioMap);
            _format = null;
            _codec = null;
            _frame = null;
            _packet = null;
            _swr = null;
            _streamIndex = -1;
            _timeBase = default;
            _outChannels = 2;
            return;
        }

        FfmpegRuntime.EnsureReady();

        _format = FfmpegInput.Open(item, AVMediaType.AVMEDIA_TYPE_AUDIO);
        ffmpeg.avformat_find_stream_info(_format, null).ThrowIfError();

        _item = item;

        AVCodec* decoder = null;
        var requestedStream = -1;
        if (item.AudioTrackIndex >= 0)
        {
            var ordinal = 0;
            for (var i = 0; i < (int)_format->nb_streams; i++)
            {
                var parameters = _format->streams[i]->codecpar;
                if (parameters == null || parameters->codec_type != AVMediaType.AVMEDIA_TYPE_AUDIO) continue;
                if (ordinal++ != item.AudioTrackIndex) continue;
                requestedStream = i;
                decoder = ffmpeg.avcodec_find_decoder(parameters->codec_id);
                break;
            }
        }

        if (requestedStream >= 0)
            _streamIndex = requestedStream;
        else
            _streamIndex = ffmpeg.av_find_best_stream(
                _format, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &decoder, 0);

        if (_streamIndex < 0 || decoder == null)
        {
            // If separate AlternateAudioUrl was tried and failed or had no audio, try falling back to FilePath
            if (!string.IsNullOrWhiteSpace(item.AlternateAudioUrl))
            {
                try
                {
                    var fallbackItem = new PlaylistItem
                    {
                        FilePath = item.FilePath,
                        SourceKind = item.SourceKind,
                        InputFormat = item.InputFormat,
                        InputOptions = item.InputOptions,
                        AudioTrackIndex = item.AudioTrackIndex,
                        AudioChannelMode = item.AudioChannelMode
                    };
                    var fallbackFormat = FfmpegInput.Open(fallbackItem, AVMediaType.AVMEDIA_TYPE_AUDIO);
                    ffmpeg.avformat_find_stream_info(fallbackFormat, null).ThrowIfError();
                    _streamIndex = ffmpeg.av_find_best_stream(fallbackFormat, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &decoder, 0);
                    if (_streamIndex >= 0 && decoder != null)
                    {
                        var oldFmt = _format;
                        _format = fallbackFormat;
                        ffmpeg.avformat_close_input(&oldFmt);
                    }
                    else
                    {
                        ffmpeg.avformat_close_input(&fallbackFormat);
                    }
                }
                catch { }
            }
        }

        if (_streamIndex < 0 || decoder == null)
        {
            var webUrl = YtDlpResolver.LooksLikeWebVideoPage(item.FilePath)
                ? item.FilePath
                : (YtDlpResolver.LooksLikeWebVideoPage(item.AlternateAudioUrl) ? item.AlternateAudioUrl : null);

            if (!string.IsNullOrWhiteSpace(webUrl))
            {
                try
                {
                    var resolved = YtDlpResolver.ResolveAsync(webUrl).GetAwaiter().GetResult();
                    var targetUrl = !string.IsNullOrWhiteSpace(resolved.AudioUrl) ? resolved.AudioUrl : resolved.MediaUrl;
                    if (!string.IsNullOrWhiteSpace(targetUrl))
                    {
                        var ytItem = new PlaylistItem
                        {
                            FilePath = targetUrl,
                            SourceKind = "URL",
                            InputFormat = item.InputFormat,
                            InputOptions = item.InputOptions,
                            AudioTrackIndex = item.AudioTrackIndex,
                            AudioChannelMode = item.AudioChannelMode
                        };
                        var ytFormat = FfmpegInput.Open(ytItem, AVMediaType.AVMEDIA_TYPE_AUDIO);
                        ffmpeg.avformat_find_stream_info(ytFormat, null).ThrowIfError();
                        var testStream = ffmpeg.av_find_best_stream(ytFormat, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &decoder, 0);
                        if (testStream >= 0 && decoder != null)
                        {
                            _streamIndex = testStream;
                            var oldFmt = _format;
                            _format = ytFormat;
                            if (oldFmt != null) ffmpeg.avformat_close_input(&oldFmt);
                        }
                        else
                        {
                            ffmpeg.avformat_close_input(&ytFormat);
                        }
                    }
                }
                catch { }
            }
        }

        if (_streamIndex < 0 || decoder == null) throw new InvalidOperationException(item.AudioTrackIndex >= 0
            ? $"Audio track {item.AudioTrackIndex} does not have a supported decoder."
            : "No audio decoder was found.");

        _codec = ffmpeg.avcodec_alloc_context3(decoder);
        if (_codec == null) throw new InvalidOperationException("avcodec_alloc_context3 failed.");
        ffmpeg.avcodec_parameters_to_context(_codec, _format->streams[_streamIndex]->codecpar).ThrowIfError();
        ffmpeg.avcodec_open2(_codec, decoder, null).ThrowIfError();
        _timeBase = _format->streams[_streamIndex]->time_base;

        _packet = ffmpeg.av_packet_alloc();
        _frame = ffmpeg.av_frame_alloc();
        if (_packet == null || _frame == null) throw new OutOfMemoryException("FFmpeg frame/packet allocation failed.");

        var inputLayout = _codec->ch_layout;
        if (inputLayout.nb_channels <= 0)
            ffmpeg.av_channel_layout_default(&inputLayout, 2);
        _outChannels = Math.Clamp(inputLayout.nb_channels, 1, 8);
        AVChannelLayout outputLayout = default;
        ffmpeg.av_channel_layout_default(&outputLayout, _outChannels);

        SwrContext* swr = null;
        ffmpeg.swr_alloc_set_opts2(
            &swr,
            &outputLayout, AVSampleFormat.AV_SAMPLE_FMT_S16, OutRate,
            &inputLayout, _codec->sample_fmt, _codec->sample_rate,
            0, null).ThrowIfError();
        if (swr == null) throw new InvalidOperationException("swr_alloc_set_opts2 failed.");
        ffmpeg.swr_init(swr).ThrowIfError();
        _swr = swr;
        ffmpeg.av_channel_layout_uninit(&outputLayout);
    }

    public void Seek(TimeSpan time)
    {
        if (_sharedAudioReader is not null) return;
        var tb = ffmpeg.av_q2d(_timeBase);
        var ts = tb > 0 ? (long)(Math.Max(0, time.TotalSeconds) / tb) : 0;
        var res = ffmpeg.av_seek_frame(_format, _streamIndex, ts, ffmpeg.AVSEEK_FLAG_BACKWARD);
        if (res < 0)
        {
            // Fallback without BACKWARD flag (some HTTP/HLS protocols require forward or default seek)
            res = ffmpeg.av_seek_frame(_format, _streamIndex, ts, 0);
        }
        if (res >= 0)
        {
            ffmpeg.avcodec_flush_buffers(_codec);
            if (_swr != null)
            {
                ffmpeg.swr_close(_swr);
                try { ffmpeg.swr_init(_swr); } catch { }
            }
            _inputEof = false;
            _flushSent = false;
            ffmpeg.av_packet_unref(_packet);
            ffmpeg.av_frame_unref(_frame);
        }
        // If res < 0, stream is unseekable via protocol. Decoder remains intact and reads sequentially.
    }

    public bool TryRead(out AudioChunk chunk)
    {
        if (_sharedAudioReader is not null)
        {
            return _sharedAudioReader.TryRead(out chunk);
        }
        while (true)
        {
            // Drain every buffered decoded audio frame before feeding another packet.
            ffmpeg.av_frame_unref(_frame);
            var receive = ffmpeg.avcodec_receive_frame(_codec, _frame);
            if (receive >= 0)
            {
                var expected = ffmpeg.swr_get_out_samples(_swr, _frame->nb_samples);
                var maxSamples = Math.Max(_frame->nb_samples * 4, Math.Max(expected, 1));
                var bytes = new byte[maxSamples * _outChannels * sizeof(short)];
                int converted;
                fixed (byte* p = bytes)
                {
                    byte** output = stackalloc byte*[1];
                    output[0] = p;
                    converted = ffmpeg.swr_convert(
                        _swr, output, maxSamples,
                        _frame->extended_data, _frame->nb_samples).ThrowIfError();
                }

                Array.Resize(ref bytes, converted * _outChannels * sizeof(short));
                bytes = AudioChannelRouter.ToStereo(bytes, _outChannels, string.IsNullOrWhiteSpace(_item.AudioChannelMode) ? "Auto Stereo" : _item.AudioChannelMode);
                var ts = _frame->best_effort_timestamp;
                var pts = ts == ffmpeg.AV_NOPTS_VALUE ? 0.0 : ts * ffmpeg.av_q2d(_timeBase);
                chunk = new AudioChunk(bytes, pts);
                return true;
            }

            if (receive == ffmpeg.AVERROR_EOF)
            {
                chunk = null!;
                return false;
            }

            if (receive != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                receive.ThrowIfError();

            if (_inputEof)
            {
                if (!_flushSent)
                {
                    var flush = ffmpeg.avcodec_send_packet(_codec, null);
                    if (flush != ffmpeg.AVERROR_EOF)
                        flush.ThrowIfError();
                    _flushSent = true;
                    continue;
                }

                chunk = null!;
                return false;
            }

            while (true)
            {
                ffmpeg.av_packet_unref(_packet);
                var read = ffmpeg.av_read_frame(_format, _packet);
                if (read == ffmpeg.AVERROR_EOF)
                {
                    _inputEof = true;
                    break;
                }
                if (read == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Thread.Sleep(2);
                    continue;
                }
                read.ThrowIfError();

                if (_packet->stream_index != _streamIndex)
                    continue;

                var send = ffmpeg.avcodec_send_packet(_codec, _packet);
                ffmpeg.av_packet_unref(_packet);
                if (send == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                    break;
                send.ThrowIfError();
                break;
            }
        }
    }

    public void Dispose()
    {
        if (_sharedAudioReader is not null)
        {
            _sharedAudioReader.Dispose();
            return;
        }
        var swr = _swr;
        ffmpeg.swr_free(&swr);
        var frame = _frame;
        ffmpeg.av_frame_free(&frame);
        var packet = _packet;
        ffmpeg.av_packet_free(&packet);
        var codec = _codec;
        ffmpeg.avcodec_free_context(&codec);
        var format = _format;
        ffmpeg.avformat_close_input(&format);
    }
}
