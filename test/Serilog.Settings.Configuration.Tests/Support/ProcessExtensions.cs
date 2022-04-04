using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
            RunCommand("dotnet", workingDirectory, args);
        }

        public static CommandResult RunCommand(string command, params string[] args)
        {
            return RunCommand(command, "", args);
        }

        static CommandResult RunCommand(string command, string workingDirectory, params string[] args)
        {
            var arguments = new StringBuilder(args.Select(e => e.Length + 3).Sum());
            foreach (var arg in args)
            {
                var hasSpace = arg.Contains(" ");
                if (hasSpace) arguments.Append('"');
                arguments.Append(arg);
                if (hasSpace) arguments.Append('"');
                arguments.Append(' ');
            }
            var startInfo = new ProcessStartInfo(command, arguments.ToString())
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var process = new Process { StartInfo = startInfo };
            process.Start();
            var timeout = TimeSpan.FromSeconds(30);
            var exited = process.WaitForExit((int)timeout.TotalMilliseconds);
            if (!exited)
            {
                process.Kill();
                throw new TimeoutException($"The command '{command} {arguments}' did not execute within {timeout.TotalSeconds} seconds");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            if (process.ExitCode != 0)
            {
                var message = new StringBuilder();
                message.AppendLine($"The command '{command} {arguments}' exited with code {process.ExitCode}");
                if (output.Length > 0)
                {
                    message.AppendLine("*** Output ***");
                    message.AppendLine(output);
                }
                if (error.Length > 0)
                {
                    message.AppendLine("*** Error ***");
                    message.AppendLine(error);
                }
                throw new InvalidOperationException(message.ToString());
            }

            return new CommandResult(output, error);
        }
    }
}
