using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Build
{
    internal class ProcessUtil
    {
        public static async Task<ProcessResult> RunProcess(string path, string workingDirectory, string arguments, CancellationToken cancellationToken)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = workingDirectory,
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

            return new ProcessResult(process);
        }

        public class ProcessResult : IDisposable
        {
            private readonly Process _process;
            private readonly StringBuilder _standardOutput;

            internal ProcessResult(Process process)
            {
                _process = process;
                _standardOutput = new StringBuilder();

                Task.Run(ReadStandardError);
            }

            private async Task ReadStandardError()
            {
                StandardError = await _process.StandardError.ReadToEndAsync();
            }

            public async IAsyncEnumerable<string> GetStandardOutputLines()
            {
                var output = _process.StandardOutput;
                while (true)
                {
                    var line = await output.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                    {
                        yield break;
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _standardOutput.AppendLine(line);
                        yield return line;
                    }
                }
            }

            public int ExitCode
            {
                get
                {
                    _process.WaitForExit();
                    return _process.ExitCode;
                }
            }

            public string StandardOutput => _standardOutput.ToString();
            public string? StandardError { get; private set; }

            public void Dispose() => _process.Dispose();
        }
    }
}
