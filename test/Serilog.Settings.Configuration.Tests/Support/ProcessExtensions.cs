using System;
using System.Diagnostics;

namespace Serilog.Settings.Configuration.Tests.Support
{
    public static class ProcessExtensions
    {
        public struct CommandResult
        {
            public CommandResult(string output, string error)
            {
                Output = output;
                Error = error;
            }

            public string Output { get; }
            public string Error { get; }
        }

        public static void RunDotnet(string workingDirectory, params string[] args)
        {
            RunCommand("dotnet", useShellExecute: true, workingDirectory, args);
        }

        public static CommandResult RunCommand(string command, params string[] args)
        {
            return RunCommand(command, useShellExecute: false, "", args);
        }

        static CommandResult RunCommand(string command, bool useShellExecute, string workingDirectory, params string[] args)
        {
            var arguments = $"\"{string.Join("\" \"", args)}\"";
            var redirect = !useShellExecute;
            var startInfo = new ProcessStartInfo(command, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = useShellExecute,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = redirect,
                RedirectStandardError = redirect,
            };
            var process = new Process { StartInfo = startInfo };
            process.Start();
            var timeout = TimeSpan.FromSeconds(30);
            var exited = process.WaitForExit((int)timeout.TotalMilliseconds);
            if (!exited)
            {
                throw new TimeoutException($"The command '{command} {arguments}' did not execute within {timeout.TotalSeconds} seconds");
            }

            var error = redirect ? process.StandardError.ReadToEnd() : "";
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"The command '{command} {arguments}' exited with code {process.ExitCode}");
            }

            var output = redirect ? process.StandardOutput.ReadToEnd() : "";
            return new CommandResult(output, error);
        }
    }
}
