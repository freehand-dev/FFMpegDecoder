using FFMpegDecoder.Models;
using FFMpegDecoder.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FFMpegDecoder.Services
{
    public class FFMpegOutputParser
    {
        private readonly ILogger<FFMpegOutputParser>? _logger;
        private DecoderOptions _settings;

        #region 'metrics'
        private StopwatchCounter _decklink_video_empty_counter;
        private StopwatchCounter _decklink_audio_empty_counter;
        #endregion

        public FFMpegOutputParser(IOptionsMonitor<DecoderOptions> settings, ILogger<FFMpegOutputParser> logger)
        {
            this._logger = logger;
            this._settings = settings.CurrentValue;

            this._decklink_video_empty_counter = new StopwatchCounter(50, TimeSpan.FromSeconds(10));
            this._decklink_audio_empty_counter = new StopwatchCounter(30, TimeSpan.FromSeconds(10));
        }

        public void ProcessLine(string? line, Action needRestart)
        {
            if (string.IsNullOrEmpty(line))
                return;

            try
            {
                string? id;
                string? message;

                if (FFMpegOutputParser.FFMpegParseSRT(line, out id, out message))
                {
                    _logger?.LogInformation($"SRT[{id}], message: {message}");
                }
                else if (FFMpegOutputParser.FFMpegParseMpegTS(line, out id, out message))
                {
                    _logger?.LogInformation($"MPEGTS[{id}], message: {message}");

                    if (!string.IsNullOrEmpty(message))
                    {
                        //message: New audio stream
                        if (message.Trim().ToLower().StartsWith("new audio stream") || message.Trim().ToLower().StartsWith("new video stream"))
                        {
                            this._logger?.LogWarning("Detect new stream, restart ffmpeg instance");
                            if (this._settings.FFMpeg.Watchdog.Mpegts.NewStream)
                            {
                                needRestart?.Invoke();
                            }
                                
                        }
                        else
                        // Detected PMT change (Loglevel=verbose)
                        if (message.Trim().ToLower().StartsWith("detected pmt change"))
                        {
                            this._logger?.LogWarning("Detected PMT change, restart ffmpeg instance");
                            if (this._settings.FFMpeg.Watchdog.Mpegts.PMTChange)
                            {
                                needRestart?.Invoke();
                            }
                                
                        }
                    }
                }
                else if (FFMpegOutputParser.FFMpegParseDecklink(line, out id, out message))
                {
                    _logger?.LogInformation($"Decklink[{id}], message: {message}");

                    if (!string.IsNullOrEmpty(message))
                    {
                        //message: There are not enough buffered video frames. Video may misbehave!
                        if (message.Trim().ToLower().StartsWith("there are not enough buffered video frames. video may misbehave!"))
                        {
                            this._decklink_video_empty_counter.Increment(out bool brokenThreshold, out int brokenThresholdValue);
                            if (brokenThreshold)
                            {
                                this._logger?.LogWarning($"There are not enough buffered video frames. Video may misbehave! Dub {brokenThresholdValue}");
                                if (this._settings.FFMpeg.Watchdog.Decklink.NoBufferedVideo)
                                {
                                    needRestart?.Invoke();
                                }
                            }
                        }
                        else
                        // There's no buffered audio. Audio will misbehave!
                         if (message.Trim().ToLower().StartsWith("there's no buffered audio. audio will misbehave!"))
                        {
                            this._decklink_audio_empty_counter.Increment(out bool brokenThreshold, out int brokenThresholdValue);
                            if (brokenThreshold)
                            {
                                this._logger?.LogWarning($"There's no buffered audio. Audio will misbehave! Dub {brokenThresholdValue}");
                                if (this._settings.FFMpeg.Watchdog.Decklink.NoBufferedAudio)
                                {
                                    needRestart?.Invoke();
                                }
                            }
                        }
                    }
                }
                else
                {
                    var isProgress = FFMpegOutputParser.FFMpegParseProgress(line);
                    if (isProgress == null)
                        this._logger?.LogDebug(line);
                }
            }
            catch
            {
                _logger?.LogWarning($"Error parse ffmpeg output: {line}");
            }
        }


        public static bool FFMpegParseMpegTS(string line, out string? id, out string? message)
        {
            id = default;
            message = default;
            Regex mpegtsRegex = new Regex("\\[mpegts\\s@\\s(\\w+)](.+)", RegexOptions.Compiled);
            Match match = mpegtsRegex.Match(line);
            if (match.Success)
            {
                id = match.Groups[1].Value;
                message = match.Groups[2].Value.Trim();
            }
            return match.Success;
        }

        public static bool FFMpegParseSRT(string line, out string? id, out string? message)
        {
            id = default;
            message = default;
            Regex mpegtsRegex = new Regex("\\[srt\\s@\\s(\\w+)](.+)", RegexOptions.Compiled);
            Match match = mpegtsRegex.Match(line);
            if (match.Success)
            {
                id = match.Groups[1].Value;
                message = match.Groups[2].Value.Trim();
            }
            return match.Success;
        }

        public static bool FFMpegParseDecklink(string line, out string? id, out string? message)
        {
            id = default;
            message = default;
            Regex mpegtsRegex = new Regex("\\[decklink\\s@\\s(\\w+)](.+)", RegexOptions.Compiled);
            Match match = mpegtsRegex.Match(line);
            if (match.Success)
            {
                id = match.Groups[1].Value;
                message = match.Groups[2].Value.Trim();
            }
            return match.Success;
        }


        public static FFMpegOutputProgress? FFMpegParseProgress(string line)
        {
            FFMpegOutputProgress? result = new FFMpegOutputProgress();

            // frame
            Match frameMatch = Regex.Match(line, @"frame=\s*([\w\.\:\/]+)\s");
            if (frameMatch.Success)
            {
                if (int.TryParse(frameMatch.Groups[1].Value.Trim(), out int frameParse))
                {
                    result.Frame = frameParse;
                }
            }


            // fps
            Match fpsMatch = Regex.Match(line, @"fps=\s*([\w\.\:\/]+)\s");
            if (fpsMatch.Success)
            {
                if (double.TryParse(fpsMatch.Groups[1].Value.Trim(), out double fpsParse))
                {
                    result.FPS = fpsParse;
                }
            }


            // size
            Match sizeMatch = Regex.Match(line, @"size=\s*([\w\.\:\/]+)\s");
            if (sizeMatch.Success)
            {
                result.Size = sizeMatch.Groups[1].Value.Trim();
            }


            // bitrate
            Match bitrateMatch = Regex.Match(line, @"bitrate=\s*([\w\.\:\/]+)\s");
            if (bitrateMatch.Success)
            {
                result.Bitrate = bitrateMatch.Groups[1].Value.Trim();
            }


            // time
            Match timeMatch = Regex.Match(line, @"time=\s*([\w\.\:\/]+)\s");
            if (timeMatch.Success)
            {
                if (TimeSpan.TryParse(timeMatch.Groups[1].Value.Trim(), CultureInfo.InvariantCulture, out var parseTime))
                {
                    result.Time = parseTime;
                }
            }


            // dup
            Match dupMatch = Regex.Match(line, @"dup=\s*([\w\.\:\/]+)\s");
            if (dupMatch.Success)
            {
                if (int.TryParse(dupMatch.Groups[1].Value.Trim(), out int dupParse))
                {
                    result.Dup = dupParse;
                }
            }

            // drop
            Match dropMatch = Regex.Match(line, @"drop=\s*([\w\.\:\/]+)\s");
            if (dropMatch.Success)
            {
                if (int.TryParse(dropMatch.Groups[1].Value.Trim(), out int dropParse))
                {
                    result.Drop = dropParse;
                }
            }

            // speed
            Match speedMatch = Regex.Match(line, @"speed=\s*([\w\.\:\/]+)\s");
            if (speedMatch.Success)
            {
                result.Speed = speedMatch.Groups[1].Value.Trim();
            }


            return frameMatch.Success && bitrateMatch.Success ? result : null;
        }
    }
}
