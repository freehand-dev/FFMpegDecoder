using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FFMpegDecoder.Models
{
    public class FFMpegArgumentBuilder
    {
        public enum DecklinkOutputFormat
        {
            UYVY422_1920i50,
            UYVY422_720p25,
            UYVY422_720p50,
            UYVY422_1920p25,
            UYVY422_1920p50
        }
        public string? Progress { get; set; }
        public string Input { get; set; }
        public bool IgnoreUnknown { get; set; } = true;      
        public bool HideBanner { get; set; } = true;
        public string Loglevel { get; set; } = "warning";
        public int? RtBufSize { get; set; }
        public int? ProbeSize { get; set; }
        public int? AnalyzeDuration { get; set; }
        public List<string> Flags { get; set; } = new List<string>();
        public List<string> FFlags { get; set; } = new List<string>();
        public string? FilterComplex { get; set; }
        public List<string> Maps { get; set; } = new List<string>();
        public string Output{ get; set; }
        public DecklinkOutputFormat OutputFormat { get; set; }

        public FFMpegArgumentBuilder(string input, string output)
        {
            this.Input = input;
            this.Output = output;
        }

        public override string ToString()
        {
            #region "validation"
            if (this.Input == null)
            {
                throw new Exception("Input is empty");
            }

            if (string.IsNullOrEmpty(this.Output))
            {
                throw new Exception("Output is empty");
            }
            #endregion

            List<string> args = new List<string>();

            //args.Add("-err_detect ignore_err");


            if (this.HideBanner)
                args.Add("-hide_banner");

            if (this.IgnoreUnknown)
                args.Add("-ignore_unknown");

            args.Add($"-loglevel {(string.IsNullOrEmpty(this.Loglevel) ? "warning"  : this.Loglevel)}");

            if (this.Progress != null)
                args.Add($"-progress \"{this.Progress?.ToString()}\"");

            if (this.RtBufSize.HasValue)
                args.Add($"-rtbufsize {this.RtBufSize}");

            if (this.AnalyzeDuration.HasValue)
                args.Add($"-analyzeduration {this.AnalyzeDuration}");

            if (this.ProbeSize.HasValue)
                args.Add($"-probesize {this.ProbeSize}");

            foreach (string flag in this.Flags)
            {
                args.Add($"-flags {flag}");
            }

            foreach (string flag in this.FFlags)
            {
                args.Add($"-fflags {flag}");
            }

            args.Add($"-f mpegts");
            args.Add($"-i \"{this.Input}\"");


            if (!string.IsNullOrEmpty(this.FilterComplex))
                args.Add($"-filter_complex \"{this.FilterComplex}\"");

            foreach (string map in this.Maps)
            {
                args.Add($"-map {map}");
            }

            // https://www.ffmpeg.org/ffmpeg-all.html#tinterlace
            // args.Add($"-vf \"tinterlace=interleave_top\"");

            args.Add($"-f decklink");

            switch (OutputFormat)
            {
                case DecklinkOutputFormat.UYVY422_1920i50:
                    args.Add($"-field_order tb");
                    args.Add("-pix_fmt uyvy422");
                    args.Add("-s 1920x1080");
                    args.Add("-r 25000/1000");
                    break;
                case DecklinkOutputFormat.UYVY422_720p25:
                    args.Add($"-field_order progressive");
                    args.Add("-pix_fmt uyvy422");
                    args.Add("-s 1280x720");
                    args.Add("-r 25000/1000");
                    break;
                case DecklinkOutputFormat.UYVY422_720p50:
                    
                    args.Add($"-field_order progressive");
                    args.Add("-pix_fmt uyvy422");
                    args.Add("-s 1280x720");
                    args.Add("-r 50000/1000");
                    break;
                case DecklinkOutputFormat.UYVY422_1920p25:
                    args.Add($"-field_order progressive");
                    args.Add("-pix_fmt uyvy422");
                    args.Add("-s 1920x1080");
                    args.Add("-r 25000/1000");
                    break;
                case DecklinkOutputFormat.UYVY422_1920p50:
                    args.Add($"-field_order progressive");
                    args.Add("-pix_fmt uyvy422");
                    args.Add("-s 1920x1080");
                    args.Add("-r 50000/1000");
                    break;
            }

            args.Add($"\"{this.Output}\"");

            return String.Join(" ", args);
        }
    } 
}






