using Microsoft.Extensions.Options;
using FFMpegDecoder.Models;
using FFMpegDecoder.Infrastructure.Metrics;
using System.Diagnostics;
using System.Text;
using System.Xml.XPath;
using System.Xml.Linq;
using FFMpegDecoder;
using System.Collections.Generic;
using System.Xml;
using System.Reflection.Metadata;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.IO;
using FFMpegDecoder.Services;

namespace FFMpegDecoder.Services
{
    /// <summary>
    /// -loglevel verbose -channels 8 -format_code Hi50 -raw_format uyvy422 -video_input sdi -audio_input embedded -draw_bars 0 -queue_size 1073741824 -f decklink -i "DeckLink Duo (4)" 
    /// -filter_complex "[0:a]channelmap=map=0|1:stereo[ch1];[0:a] channelmap=map=2|3:stereo[ch2]" -map 0:v -map [ch1] -map [ch2] 
    /// -c:0 libx264 -b:0 8000k -preset:0 faster -profile:0 high -level:0 4.0 -minrate:0 8000k -maxrate:0 8000k -bufsize:0 700k -pix_fmt:0 yuv420p -aspect:0 16:9 -x264-params:0 nal-hrd=cbr -top:0 1 -flags:0 +ilme+ildct+cgop 
    /// -c:1 libfdk_aac -b:1 192k -c:2 libfdk_aac -b:2 192k 
    /// -bsf:v h264_mp4toannexb -flush_packets 0 -rtbufsize 2000M 
    /// -f mpegts -mpegts_transport_stream_id 1 -mpegts_original_network_id 1 -mpegts_service_id 1 -muxrate 9000k -mpegts_start_pid 336 -mpegts_pmt_start_pid 4096 -pcr_period 20 -pat_period 0.10 -sdt_period 0.25 -nit_period 0.5 -metadata service_name=OBVan -metadata service_provider=ES -metadata title=BabyBird -mpegts_flags +pat_pmt_at_frames+system_b+nit 
    /// "srt://193.239.153.205:9103?ipttl=15&latency=3000&mode=caller&payload_size=1456&transtype=live"
    /// </summary>
    public class FFMpegWorker : BackgroundService, IDisposable
    {
        static readonly string FFBinaryFolder = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg-bin");

        private readonly ILogger<FFMpegWorker> _logger;

        private readonly FFMpegOutputParser _ffMpegOutputParser;

        private DecoderOptions _settings;
        private CancellationTokenSource? _ffmpegProcessorTokenSource;

        public FFMpegWorker(ILogger<FFMpegWorker> logger, IOptionsMonitor<DecoderOptions> settings, FFMpegOutputParser ffMpegOutputParser)
        {
            this._logger = logger;
            this._settings = settings.CurrentValue;
            this._ffMpegOutputParser = ffMpegOutputParser;

            settings.OnChange(settings => 
            {
                _logger?.LogInformation("Setting changes detected");
                _settings = settings;

                // stop current ffmpeg instance
                _ffmpegProcessorTokenSource?.Cancel();
            });
        }

        public override void Dispose()
        {
            _ffmpegProcessorTokenSource?.Cancel();
            base.Dispose();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FFMpegWorker service running at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                this._ffmpegProcessorTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                CancellationToken token = _ffmpegProcessorTokenSource.Token;


                _logger.LogInformation("FFMpegWorker.Start");
                try
                {
                    _logger.LogInformation($"FFMpegWorker.Input: {_settings.Input}");
                    _logger.LogInformation($"FFMpegWorker.Output: {_settings.Output.Device}");
                    _logger.LogInformation($"FFMpegWorker.AudioMapping: {_settings.AudioMapping}");
                    
                    FFMpegArgumentBuilder ffmpegArgumetBuilder = new FFMpegArgumentBuilder(_settings.Input ?? throw new Exception("Input is null"), _settings.Output.Device ?? throw new Exception("Output is null"))
                    {
                        Progress =$"\\\\.\\pipe\\{ FFMpegProgressListener.FFMpegProgresPipeName }",
                        RtBufSize = _settings.FFMpeg.RtBufSize,
                        AnalyzeDuration = _settings.FFMpeg.AnalyzeDuration,
                        ProbeSize = _settings.FFMpeg.ProbeSize,
                        OutputFormat = _settings.Output?.Format ?? FFMpegArgumentBuilder.DecklinkOutputFormat.UYVY422_1920i50,
                        HideBanner = _settings.FFMpeg.HideBanner,
                        IgnoreUnknown = _settings.FFMpeg.IgnoreUnknown,
                        Loglevel = _settings.FFMpeg.Loglevel,
                    };

                    // -flags
                    foreach (string flag in _settings.FFMpeg.Flags)
                    {
                        ffmpegArgumetBuilder.Flags.Add(flag);
                    }

                    // -fflags
                    foreach (string fflag in _settings.FFMpeg.FFlags)
                    {
                        ffmpegArgumetBuilder.FFlags.Add(fflag);
                    }

                    ffmpegArgumetBuilder.Maps.Add("0:v");
                    ffmpegArgumetBuilder.Maps.Add("[a]");

                    #region 'ffprobe audio streams, build audio filter_complex'

                    _logger.LogInformation($"FFMpegWorker.FFprobe.Run: {ffmpegArgumetBuilder.Input}");
                    FFProbeArgumentBuilder ffprobeArgumentBuilder = new FFProbeArgumentBuilder(ffmpegArgumetBuilder.Input)
                    {
                        AnalyzeDuration = _settings.FFProbe.AnalyzeDuration,
                        ProbeSize = _settings.FFProbe.ProbeSize,
                        PrintFormat = FFProbeArgumentBuilder.FFProbePrintFormat.xml
                    };
                    _logger.LogInformation($"FFMpegWorker.FFprobe.Run.Args: {ffprobeArgumentBuilder.ToString()}");

                    List<string> audioStreams = new List<string>();
                    XDocument? ffprobeXML  = await FFProbeHelper.Run(ffprobeArgumentBuilder, _settings.FFProbe.Timeout, token);
                    if (ffprobeXML != null)
                    {
                        _logger.LogDebug($"FFMpegWorker.FFprobe.ResultXML={ffprobeXML.ToString()}");
                        XPathNavigator nav = ffprobeXML.CreateNavigator();

                        // streams count
                        var streamCountXML = nav.Select(@"//ffprobe/streams/stream");
                        int streamCount = streamCountXML?.Count ?? 0;
                        if (streamCount == 0)
                        {
                            _logger.LogError("FFMpegWorker.FFprobe: no any streams detected");
                            continue;
                        }

                        // audio stereo streams count
                        // var streamsXML = nav.Select(@"//stream[@codec_type='audio' and @channels='2' and (@codec_name='mp2' or @codec_name='aac' or @codec_name='mp3')]/@index");
                        var streamsXML = nav.Select(@"//stream[@codec_type='audio' and @channels='2' and (@codec_name='mp2' or @codec_name='aac')]/@index");
                        while (streamsXML.MoveNext())
                        {
                            if (Int32.TryParse(streamsXML.Current?.Value, out int index))
                            {
                                audioStreams.Add($"[0:{index}]channelmap=map=0:mono[ch{audioStreams.Count+1}]");
                                audioStreams.Add($"[0:{index}]channelmap=map=1:mono[ch{audioStreams.Count+1}]");
                            }
                        }
                    }

                    while (audioStreams.Count < 16)
                    {
                        audioStreams.Add($"anullsrc=r=48000:cl=mono[ch{audioStreams.Count + 1}]");
                    }


                    string amarge = $"{_settings.AudioMapping} amerge=inputs=16[a]";
                    ffmpegArgumetBuilder.FilterComplex = $"{String.Join(';', audioStreams)};{amarge}";
                    _logger.LogInformation($"FFMpegWorker.FFprobe.FilterComplexBuilder={ffmpegArgumetBuilder.FilterComplex}");
                    #endregion

                    // FFMpegOutputParser
                    DataReceivedEventHandler dataReceivedEvent = new DataReceivedEventHandler((sender, e) =>
                    {
                        this._ffMpegOutputParser?.ProcessLine(e.Data, 
                            () => {
                                _logger?.LogInformation("FFMpegOutputParser canceled action");

                                // stop current ffmpeg instance
                                _ffmpegProcessorTokenSource?.Cancel();
                            });
                    });

                    _logger.LogDebug($"FFMpegWorker.FFmpeg.RunAsync: {ffmpegArgumetBuilder.ToString()}");
                    var exitCode = await FFMpegHelper.Run(ffmpegArgumetBuilder.ToString(), dataReceivedEvent, token);
                    _logger.LogDebug($"FFMpegWorker.FFmpeg.ExitCode: {exitCode}");

                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning($"FFMpegWorker.TaskCanceled");
                }
                catch (System.OperationCanceledException)
                {
                    _logger.LogWarning($"FFMpegWorker.OperationCanceled");
                }
                catch (System.TimeoutException)
                {
                    _logger.LogWarning($"FFMpegWorker.OperationTimeout");
                }
                catch (Exception e)
                {
                    _logger.LogError($"FFMpegWorker.Exception: { e.Message } {e.GetType()}");
                } 
                finally
                {
                    // OpenTelemetry metric 
                    FFMpegProgressMeter.FFMpegInstanceRestart.Add(1);

                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogWarning($"FFMpegWorker.Stop");
        }



        
    }
}