using FFMpegDecoder.Models;
using OpenTelemetry.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace FFMpegDecoder.Models
{
    public class DecoderOptions
    {
        public string? Input { get; set; }
        public AudioMappingOptioms AudioMapping { get; set; } = new AudioMappingOptioms();
        public OutputOptions Output { get; set; } = new OutputOptions();
        public FFMpegOptions FFMpeg { get; set; } = new FFMpegOptions();
        public FFProbeOptions FFProbe { get; set; } = new FFProbeOptions();

    }

    public class AudioMappingOptioms
    {
        public enum ChannelName
        {
            ch1,
            ch2,
            ch3,
            ch4,
            ch5,
            ch6,
            ch7,
            ch8,
            ch9,
            ch10,
            ch11,
            ch12,
            ch13,
            ch14,
            ch15,
            ch16
        };

        public ChannelName ch1 { get; set; } = ChannelName.ch1;
        public ChannelName ch2 { get; set; } = ChannelName.ch2;
        public ChannelName ch3 { get; set; } = ChannelName.ch3;
        public ChannelName ch4 { get; set; } = ChannelName.ch4;
        public ChannelName ch5 { get; set; } = ChannelName.ch5;
        public ChannelName ch6 { get; set; } = ChannelName.ch6;
        public ChannelName ch7 { get; set; } = ChannelName.ch7;
        public ChannelName ch8 { get; set; } = ChannelName.ch8;
        public ChannelName ch9 { get; set; } = ChannelName.ch9;
        public ChannelName ch10 { get; set; } = ChannelName.ch10;
        public ChannelName ch11 { get; set; } = ChannelName.ch11;
        public ChannelName ch12 { get; set; } = ChannelName.ch12;
        public ChannelName ch13 { get; set; } = ChannelName.ch13;
        public ChannelName ch14 { get; set; } = ChannelName.ch14;
        public ChannelName ch15 { get; set; } = ChannelName.ch15;
        public ChannelName ch16 { get; set; } = ChannelName.ch16;

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append($"[{this.ch1}]");
            result.Append($"[{this.ch2}]");
            result.Append($"[{this.ch3}]");
            result.Append($"[{this.ch4}]");
            result.Append($"[{this.ch5}]");
            result.Append($"[{this.ch6}]");
            result.Append($"[{this.ch7}]");
            result.Append($"[{this.ch8}]");
            result.Append($"[{this.ch9}]");
            result.Append($"[{this.ch10}]");
            result.Append($"[{this.ch11}]");
            result.Append($"[{this.ch12}]");
            result.Append($"[{this.ch13}]");
            result.Append($"[{this.ch14}]");
            result.Append($"[{this.ch15}]");
            result.Append($"[{this.ch16}]");
            return result.ToString();
        }

    }

    public class FFMpegOptions
    {
        public bool HideBanner { get; set; } = true;
        public bool IgnoreUnknown { get; set; } = true;
        public string Loglevel { get; set; } = "warning";

        /// <summary>
        /// Set max memory used for buffering real-time frames.
        /// </summary>
        public int? RtBufSize { get; set; } = 0;

        /// <summary>
        /// Specify how many microseconds are analyzed to probe the input. 
        /// A higher value will enable detecting more accurate information, but will increase latency. 
        /// It defaults to 5,000,000 microseconds = 5 seconds.
        /// </summary>
        public int? AnalyzeDuration { get; set; }

        /// <summary>
        /// Set probing size in bytes, i.e. the size of the data to analyze to get stream information. 
        /// A higher value will enable detecting more information in case it is dispersed into the stream, but will increase latency.
        /// Must be an integer not lesser than 32. It is 5000000 by default. (5Mb)
        /// </summary>
        public int? ProbeSize { get; set; }
        public List<string> Flags { get; set; } = new List<string>();
        public List<string> FFlags { get; set; } = new List<string>();

        public Watchdog Watchdog { get; set; } = new Watchdog();
    }

    public class Watchdog
    {
        public WatchdogMpegts Mpegts { get; set; } = new WatchdogMpegts();
        public WatchdogDecklink Decklink { get; set; } = new WatchdogDecklink();
    }

    public class WatchdogMpegts
    {
        public bool PMTChange { get; set; } = true;
        public bool NewStream { get; set; } = true;
    }

    public class WatchdogDecklink
    {
        public bool NoBufferedAudio { get; set; } = true;
        public bool NoBufferedVideo { get; set; } = true;
    }

    public class FFProbeOptions
    {

        /// <summary>
        /// Specify how many microseconds are analyzed to probe the input. 
        /// A higher value will enable detecting more accurate information, but will increase latency. 
        /// It defaults to 5,000,000 microseconds = 5 seconds.
        /// </summary>
        public int? AnalyzeDuration { get; set; }

        /// <summary>
        /// Set probing size in bytes, i.e. the size of the data to analyze to get stream information. 
        /// A higher value will enable detecting more information in case it is dispersed into the stream, but will increase latency.
        /// Must be an integer not lesser than 32. It is 5000000 by default. (5Mb)
        /// </summary>
        public int? ProbeSize { get; set; }
        public int Timeout { get; set; } = 15000;
    }

    public class OutputOptions
    {
        public string? Device { get; set; }
        public FFMpegArgumentBuilder.DecklinkOutputFormat Format { get; set; } = FFMpegArgumentBuilder.DecklinkOutputFormat.UYVY422_1920i50;
    }
}
