using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FFMpegDecoder.Models
{
    public class FFProbeArgumentBuilder
    {
        public enum FFProbePrintFormat
        {
            xml,
            json,
        }

        public string Input { get; set; }
        public string Loglevel { get; set; } = "quiet";
        public FFProbePrintFormat PrintFormat { get; set; } = FFProbePrintFormat.json;
        public bool ShowFormat { get; set; } = true;
        public bool ShowStreams { get; set; } = true;
        public int? ProbeSize { get; set; }
        public int? AnalyzeDuration { get; set; }


        public FFProbeArgumentBuilder(string input)
        {
            this.Input = input;
        }

        public override string ToString()
        {
            #region "validation"
            if (this.Input == null)
            {
                throw new Exception("Input is empty");
            }
            #endregion

            List<string> args = new List<string>();

            args.Add($"-loglevel {(string.IsNullOrEmpty(this.Loglevel) ? "quiet" : this.Loglevel)}");

            args.Add($"-print_format \"{this.PrintFormat}\"");

            if (this.ShowFormat)
                args.Add("-show_format");

            if (this.ShowStreams)
                args.Add("-show_streams");

            if (this.AnalyzeDuration.HasValue)
                args.Add($"-analyzeduration {this.AnalyzeDuration}");

            if (this.ProbeSize.HasValue)
                args.Add($"-probesize {this.ProbeSize}");

            args.Add($"-i \"{this.Input}\"");

            return String.Join(" ", args);
        }
    } 
}






