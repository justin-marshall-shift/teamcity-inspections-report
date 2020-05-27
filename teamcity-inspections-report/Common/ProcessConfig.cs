using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ToolKit.Common
{
    public class ProcessConfig
    {
        public ProcessConfig(string executable, string arguments, Action<string, bool> messageHandler, string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            Executable = executable;
            Arguments = arguments;
            MessageHandler = messageHandler;
        }

        public string WorkingDirectory { get; }
        public string Executable { get; }
        public string Arguments { get; }
        public Action<string,bool> MessageHandler { get; }
    }

    public static class ProcessUtils
    {
        public static async Task RunProcess(ProcessConfig config)
        {
            var info = new ProcessStartInfo
            {
                FileName = config.Executable,
                Arguments = config.Arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true, 
                WorkingDirectory = config.WorkingDirectory
            };

            var tcs = new TaskCompletionSource<int>();
            var outputTcs = new TaskCompletionSource<int>();
            var errorTcs = new TaskCompletionSource<int>();

            var externalProcess = new Process
            {
                StartInfo = info,
                EnableRaisingEvents = true
            };

            externalProcess.OutputDataReceived += (sender, args) =>
            {
                if (args != null)
                {
                    config.MessageHandler.Invoke(args.Data, false);
                }
                else
                {
                    outputTcs.SetResult(0);
                }
            };

            externalProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args != null)
                {
                    config.MessageHandler.Invoke(args.Data, true);
                }
                else
                {
                    errorTcs.SetResult(0);
                }
            };

            externalProcess.Exited += (sender, args) =>
            {
                tcs.SetResult(externalProcess.ExitCode);
                externalProcess.Dispose();
            };

            externalProcess.Start();
            externalProcess.BeginOutputReadLine();
            externalProcess.BeginErrorReadLine();
            
            _ = Task.Run(async () => await Task.WhenAll(outputTcs.Task, errorTcs.Task));
            await tcs.Task;
        }
    }
}
