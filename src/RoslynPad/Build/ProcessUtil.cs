using System.Diagnostics;
using System.Text;

namespace RoslynPad.Build;

internal class ProcessUtil
{
    public static async Task<ProcessResult> RunProcessAsync(string path, string workingDirectory, string arguments, CancellationToken cancellationToken)
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
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };

        var exitTcs = new TaskCompletionSource<object?>();
        process.Exited += (_, _) => exitTcs.TrySetResult(null);

        using var _ = cancellationToken.Register(() =>
        {
            try
            {
                exitTcs.TrySetCanceled();
                process.Kill();
            }
            catch { }
        });

        await Task.Run(process.Start).ConfigureAwait(false);

        return new ProcessResult(process, exitTcs);
    }

    public class ProcessResult : IDisposable
    {
        private readonly Process _process;
        private readonly TaskCompletionSource<object?> _exitTcs;
        private readonly StringBuilder _standardOutput;

        internal ProcessResult(Process process, TaskCompletionSource<object?> exitTcs)
        {
            _process = process;
            _exitTcs = exitTcs;
            _standardOutput = new StringBuilder();
            _ = Task.Run(ReadStandardErrorAsync);
        }

        private async Task ReadStandardErrorAsync() =>
            StandardError = await _process.StandardError.ReadToEndAsync().ConfigureAwait(false);

        public Task WaitForExitAsync() => _exitTcs.Task;

        public async IAsyncEnumerable<string> GetStandardOutputLinesAsync()
        {
            var output = _process.StandardOutput;
            while (true)
            {
                var line = await output.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    await _exitTcs.Task.ConfigureAwait(false);
                    yield break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    _standardOutput.AppendLine(line);
                    yield return line;
                }
            }
        }

        public int ExitCode => _process.ExitCode;

        public string StandardOutput => _standardOutput.ToString();
        public string? StandardError { get; private set; }

        public void Dispose() => _process.Dispose();
    }
}
