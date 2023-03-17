using FFMpegDecoder.Models;
using FFMpegDecoder.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FFMpegDecoder
{
    public static class FFProbeHelper
    {
        static public string FFProbePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg-bin");
        static public string FFProbeExe { get; set; } = "ffprobe.exe";

        static public async Task<XDocument?> Run(FFProbeArgumentBuilder argumentBuilder, int timeout = 5000, CancellationToken cancellationToken = default)
        {
            string ffprobeApp = Path.Combine(FFProbePath, FFProbeExe);

            argumentBuilder.PrintFormat = FFProbeArgumentBuilder.FFProbePrintFormat.xml;

            if (!File.Exists(ffprobeApp))
            {
                throw new Exception($"ffprobe could not be found {ffprobeApp}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobeApp,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            

            startInfo.Arguments = argumentBuilder.ToString();


            var xml = new StringBuilder();

            var dataReceived = new DataReceivedEventHandler((sender, e) =>
            {
                if (e.Data != null)
                    xml.AppendLine(e.Data);
            });

            Process ffProbeProcess = new Process();
            try
            {
                ffProbeProcess.StartInfo = startInfo;
                ffProbeProcess.OutputDataReceived += dataReceived;

                ffProbeProcess.Start();
                ffProbeProcess.BeginOutputReadLine();
                try
                {
                    await ffProbeProcess.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromMilliseconds(timeout));
                }
                finally
                {
                    if (ffProbeProcess != null && !ffProbeProcess.HasExited)
                    {
                        try
                        {
                            ffProbeProcess.Kill();
                        }
                        catch { }
                    }
                }

                return XDocument.Parse(xml.ToString());
            }
            finally
            {
                ffProbeProcess.OutputDataReceived -= dataReceived;
                ffProbeProcess?.Dispose();
            }
        }
    }
}
