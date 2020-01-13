using System;
using System.Text;
using UnityEngine;

namespace NpmPublisherSupport
{
    public delegate void NpmCommandCallback(int? code, string result);

    public static class NpmUtils
    {
        private static readonly StringBuilder Error = new StringBuilder();
        private static readonly StringBuilder Output = new StringBuilder();

        public static bool IsNpmRunning { get; private set; }


        public static string WorkingDirectory { get; set; }

        public static void ExecuteNpmCommand(string args, NpmCommandCallback callback)
        {
            if (IsNpmRunning)
                throw new InvalidOperationException("Npm instance already running");

            if (WorkingDirectory == null)
                throw new InvalidOperationException("WorkingDirectory is null");

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                Arguments = args,
                CreateNoWindow = true,
                FileName = "npm.cmd",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = WorkingDirectory,
            };

            var launchProcess = System.Diagnostics.Process.Start(startInfo);
            if (launchProcess == null || launchProcess.HasExited || launchProcess.Id == 0)
            {
                var msg = "No 'npm' executable was found. Please install Npm on your system and restart computer";
                Debug.LogError(msg);
                callback(null, msg);
            }
            else
            {
                IsNpmRunning = true;
                Error.Length = 0;
                Output.Length = 0;
                launchProcess.OutputDataReceived += (sender, e) => Output.AppendLine(e.Data ?? "");
                launchProcess.ErrorDataReceived += (sender, e) => Error.AppendLine(e.Data ?? "");
                launchProcess.Exited += (sender, e) =>
                {
                    IsNpmRunning = false;
                    bool success = 0 == launchProcess.ExitCode;
                    if (!success)
                    {
                        var err = Error.ToString();
                        Debug.LogError($"npm {args}\n\nExitCode: {launchProcess.ExitCode}\n\n{err}");
                        callback(launchProcess.ExitCode, err);
                    }
                    else
                    {
                        var msg = Output.ToString();
                        callback(0, msg);
                    }
                };

                launchProcess.BeginOutputReadLine();
                launchProcess.BeginErrorReadLine();
                launchProcess.EnableRaisingEvents = true;
            }
        }
    }
}