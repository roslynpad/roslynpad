using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Build
{
    internal class ProcessUtil
    {
        public static async Task<ProcessResult> RunProcess(string path, string arguments, CancellationToken cancellationToken)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            };

            using var _ = cancellationToken.Register(() =>
            {
                try { process.Kill(); }
                catch { }
            });

            await Task.Run(() => process.Start()).ConfigureAwait(false);

            var outputs = await Task.WhenAll(
                    process.StandardOutput.ReadToEndAsync(),
                    process.StandardError.ReadToEndAsync())
                .ConfigureAwait(false);

            return new ProcessResult(process.ExitCode, outputs[0], outputs[1]);
        }

        public class ProcessResult
        {
            public ProcessResult(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public int ExitCode { get; }
            public string StandardOutput { get; }
            public string StandardError { get; }
        }
    }
}
