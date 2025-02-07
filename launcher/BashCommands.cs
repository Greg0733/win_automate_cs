using Microsoft.WindowsAppSDK.Runtime.Packages;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace launcher
{
    internal class BashCommands
    {
        public static readonly Regex pythonVersionRegex = new(@"([0-9]+)\.([0-9]+)\.([0-9]+)");

        private static void ReportStreamLines(StreamReader streamReader, IProgress<string> streamLines)
        {
            for (string? line = streamReader.ReadLine(); line != null; line = streamReader.ReadLine())
            {
                streamLines.Report(line);
            }
        }

        private static string RunProcessInternal(string exePath, string args, string workingDir = ".", bool captureOutput = false, bool admin = false, bool waitForExit = true, IProgress<string>? outputLines = null)
        {
            if (captureOutput && (admin || outputLines != null))
            {
                throw new ArgumentException("Unable to capture output of an admin process, because shell is required for admin privileges and forbidden for capturing output");
            }

            if (captureOutput && outputLines != null)
            {
                throw new ArgumentException($"Unable to capture output if it is already forwarded to the {outputLines.GetType().Name} object");
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = exePath,
                Arguments = args,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            if (captureOutput || outputLines != null)
            {
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
            }
            if (admin)
            {
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
            }

            Process process = new()
            {
                StartInfo = startInfo
            };
            process.Start();

            if (outputLines != null)
            {
                Task stdOutTask = Task.Run(() => ReportStreamLines(process.StandardOutput, outputLines));
                Task stdErrTask = Task.Run(() => ReportStreamLines(process.StandardError, outputLines));

                stdOutTask.Wait();
                stdErrTask.Wait();
            }

            if (captureOutput)
            {
                return process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            }

            if (waitForExit) process.WaitForExit();
            return "";
        }

        public static void RunExe(string exePath, string args = "", string workingDir = ".", bool admin = false, bool waitForExit = true)
        {
            RunProcessInternal(exePath, args, workingDir, admin: admin, waitForExit: waitForExit);
        }

        public static string GetExeOutput(string exePath, string args, string workingDir = ".")
        {
            return RunProcessInternal(exePath, args, workingDir, captureOutput: true);
        }

        public static void MonitorExeOutput(string exePath, string args, IProgress<string> outputLines, string workingDir = ".")
        {
            RunProcessInternal(exePath, args, workingDir, outputLines: outputLines);
        }

        public static void ExecuteCommand(string cmdStr, string workingDir = ".")
        {
            RunProcessInternal("cmd.exe", $"/C {cmdStr}", workingDir, waitForExit: false);
        }

        public static void RunShortcut(string shortcutPath)
        {
            new Process()
            {
                StartInfo = new()
                {
                    FileName = shortcutPath,
                    UseShellExecute = true
                }
            }.Start();
        }

        public static Tuple<int, int>? VersionFromRegex(string versionStr, Regex versionRegex)
        {
            Match firstMatch = versionRegex.Match(versionStr);
            if (!firstMatch.Success) return null;

            int major = int.Parse(firstMatch.Groups[1].Value);
            int minor = int.Parse(firstMatch.Groups[2].Value);

            return new(major, minor);
        }

        public static bool IsVersionSuitable(int versionMajorEq, int versionMinorMin, string exePath, Regex versionFromOutput, string versionCmdArg = "--version")
        {
            string cmdOutput = GetExeOutput(exePath, versionCmdArg);

            Tuple<int, int>? version = VersionFromRegex(cmdOutput, versionFromOutput);

            return version != null && version.Item1 == versionMajorEq && version.Item2 >= versionMinorMin;
        }
    }
}
