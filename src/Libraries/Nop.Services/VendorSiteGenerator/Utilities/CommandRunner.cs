using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.VendorSiteGenerator.Utilities;

public sealed class CommandResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = "";
    public string StdErr { get; init; } = "";
    public bool Succeeded => ExitCode == 0;
}
public static class CommandRunner
{
    public static async Task<CommandResult> RunAsync(
        string fileName,
        string arguments,
        string workingDir,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {

        string command = $"{fileName} {arguments}";
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            }
        };

        process.Start();
        // Read the output
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        //var psi = new ProcessStartInfo
        //{
        //    FileName = fileName,
        //    Arguments = arguments,
        //    WorkingDirectory = workingDir,
        //    RedirectStandardOutput = true,
        //    RedirectStandardError = true,
        //    UseShellExecute = false,
        //    CreateNoWindow = true,
        //};

        //using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        //var stdOut = new StringBuilder();
        //var stdErr = new StringBuilder();

        //var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        //p.OutputDataReceived += (_, e) => { if (e.Data is not null) stdOut.AppendLine(e.Data); };
        //p.ErrorDataReceived += (_, e) => { if (e.Data is not null) stdErr.AppendLine(e.Data); };

        //if (!p.Start())
        //    throw new InvalidOperationException($"Failed to start {fileName}.");

        //p.BeginOutputReadLine();
        //p.BeginErrorReadLine();

        //using var reg = ct.Register(() => { try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { } });

        //var completed = await Task.WhenAny(
        //    tcs.Task,
        //    Task.Run(() =>
        //    {
        //        p.WaitForExit();
        //        tcs.TrySetResult(p.ExitCode);
        //    }),
        //    timeout.HasValue ? Task.Delay(timeout.Value, ct) : Task.Delay(Timeout.Infinite, ct)
        //);

        //if (completed != tcs.Task)
        //{
        //    try
        //    { if (!p.HasExited) p.Kill(entireProcessTree: true); }
        //    catch { }
        //    throw new TimeoutException($"{fileName} {arguments} timed out.");
        //}

        return new CommandResult
        {
            ExitCode = string.IsNullOrEmpty(error) ? 0 : 1,
            StdOut = output.ToString(),
            StdErr = error.ToString()
        };
    }
}
