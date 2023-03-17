using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FFMpegDecoder
{
    public static class FFMpegHelper
    {
        static public string FFProbePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg-bin");
        static public string FFProbeExe { get; set; } = "ffmpeg.exe";

        static public async Task<int?> Run(string argumets, DataReceivedEventHandler errorDataReceived, CancellationToken cancellationToken = default)
        {
            string ffmpegApp = Path.Combine(FFProbePath, FFProbeExe);
            int? exitCode;

            if (!File.Exists(ffmpegApp))
            {
                throw new Exception($"ffmpeg could not be found {ffmpegApp}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegApp,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            startInfo.Arguments = argumets;

            Process ffMpegProcess = new Process();
            try
            {
                ffMpegProcess.StartInfo = startInfo;
                ffMpegProcess.ErrorDataReceived += errorDataReceived;

                bool ffmpegStarted = ffMpegProcess.Start();
                ffMpegProcess.BeginErrorReadLine();
                try
                {
                    await ffMpegProcess.WaitForExitAsync(cancellationToken);
                }
                finally
                {
                    if (ffMpegProcess != null && !ffMpegProcess.HasExited)
                    {
                        try
                        {
                            ffMpegProcess.Kill();
                        }
                        catch { }
                    }
                }
            }
            finally
            {
                exitCode = ffMpegProcess.ExitCode;
                ffMpegProcess.OutputDataReceived -= errorDataReceived;
                ffMpegProcess?.Dispose();
            }
            return exitCode;
        }
    }
}
