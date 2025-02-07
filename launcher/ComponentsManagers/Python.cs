using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace launcher.ComponentsManagers
{
    internal class Python : ComponentManager
    {
        internal static readonly Python instance = new();
        internal const string pythonExeName = "python.exe";
        protected override ComponentManager[] Corequisites => emptyPrerequisites;
        protected override ComponentManager[] Prerequisites => emptyPrerequisites;

        protected override int NbSubSteps => 5;

        protected override double Weight => 2;

        private static readonly RegistryKey[] softwareKeys = { Registry.CurrentUser, Registry.LocalMachine };
        private const string pythonRegistryPath = "Software\\Python\\PythonCore";
        private const string pythonVersionSubkeyValueName = "Version";
        private const string pythonExecutablePathSubkeyValueName = "ExecutablePath";
        private const string pythonInstallPathSubkeyName = "InstallPath";

        private const int pythonMajor = 3;
        private const int pythonMinorMin = 10;
        private static readonly List<string> pythonCommonInstalls;

        private const string pythonInstallerUrl = "https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe";
        /*private const string pythonEmbeddedUrl = "https://www.python.org/ftp/python/3.10.11/python-3.10.11-embed-amd64.zip";
        private const string getPipUrl = "https://bootstrap.pypa.io/get-pip.py";*/

        private string? pythonPath = null;

        static Python()
        {
            pythonCommonInstalls = new();
            if (userDir != null)
            {
                pythonCommonInstalls.Add(Path.Join(userDir, "AppData", "Local", "Programs", "Python"));
            }
            if (programFilesDir != null)
            {
                pythonCommonInstalls.Add(programFilesDir);
            }
        }

        private Python() { }

        internal string GetPython()
        {
            pythonPath ??= SearchPython();

            if (pythonPath == null) throw new InvalidOperationException("Python should be installed before trying to get its path (it could not be found)");

            return pythonPath;
        }

        internal override bool CheckInstalled(IProgress<string>? logsProgress = null)
        {
            pythonPath ??= SearchPython(logsProgress);

            return pythonPath != null;
        }
        internal override void StartConfiguration() => throw new NotImplementedException();
        internal override void StartComponent() => throw new NotImplementedException();

        protected override async Task ComponentInstallAsync()
        {
            ReportInstallProgress($"Installing python {pythonMajor}.{pythonMinorMin}");
            try
            {
                string pythonInstallerPath = Path.Join(tempDir, "pythonInstaller.exe");
                ReportInstallProgress($"Downloading python installer");
                using (Stream pythonInstallerStream = await httpClientInstance.GetStreamAsync(new Uri(pythonInstallerUrl)))
                {
                    ReportInstallProgress($"Extracting python installer");
                    using FileStream pythonInstallerFile = new(pythonInstallerPath, FileMode.Create);
                    await pythonInstallerStream.CopyToAsync(pythonInstallerFile);
                }

                ReportInstallProgress($"Running python installer");
                BashCommands.RunExe(pythonInstallerPath, "/quiet AssociateFiles=0 CompileAll=1 Shortcuts=0 Include_doc=0 Include_launcher=0 Include_tcltk=0 Include_test=0");

                File.Delete(pythonInstallerPath);
            }
            catch (HttpRequestException e)
            {
                ReportInstallProgress($"Http request failed : {e.Message}");
                throw;
            }

            pythonPath = SearchPython(null);
            if (pythonPath == null)
            {
                throw new IOException("Python not found even after running installer");
            }

            ReportInstallProgress($"Installed python at {pythonPath}", true);
        }

        private static string? SearchPython(IProgress<string>? logsProgress = null)
        {
            // 1) Check python on path
            try
            {
                if (BashCommands.IsVersionSuitable(pythonMajor, pythonMinorMin, pythonExeName, BashCommands.pythonVersionRegex))
                {
                    logsProgress?.Report("Found python on path");
                    return pythonExeName;
                }
                logsProgress?.Report("Python not on path");
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == WIN32_FILE_NOT_FOUND || e.NativeErrorCode == WIN32_PATH_NOT_FOUND)
                {
                    logsProgress?.Report("Python not on path");
                }
                else
                {
                    throw;
                }
            }

            // 2) Check registry
            foreach (RegistryKey registryKey in softwareKeys)
            {
                using RegistryKey? pythonCoreKey = registryKey.OpenSubKey(pythonRegistryPath);
                if (pythonCoreKey == null) continue;

                foreach (string currKeyName in pythonCoreKey.GetSubKeyNames())
                {
                    using RegistryKey? currKey = pythonCoreKey.OpenSubKey(currKeyName);
                    if (currKey == null) continue;

                    if (currKey.GetValue(pythonVersionSubkeyValueName) is not string currPyVersionStr) continue;

                    Tuple<int, int>? currPyVersion = BashCommands.VersionFromRegex(currPyVersionStr, BashCommands.pythonVersionRegex);
                    if (currPyVersion == null) continue;

                    if (currPyVersion.Item1 == pythonMajor && currPyVersion.Item2 >= pythonMinorMin)
                    {
                        using RegistryKey? installPathKey = currKey.OpenSubKey(pythonInstallPathSubkeyName);
                        if (installPathKey == null) continue;

                        if (installPathKey.GetValue(pythonExecutablePathSubkeyValueName) is not string currPythonPath) continue;

                        logsProgress?.Report($"Found python at {currPythonPath} from registry");
                        return currPythonPath;
                    }
                }
            }
            logsProgress?.Report($"Python not found in registry");

            // 3) Check common install locations
            foreach (string commonInstallPath in pythonCommonInstalls)
            {
                if (!Directory.Exists(commonInstallPath)) continue;

                foreach (string fullDirPath in Directory.EnumerateDirectories(commonInstallPath))
                {
                    string currPythonPath = Path.Join(fullDirPath, pythonExeName);
                    if (!File.Exists(currPythonPath)) continue;

                    if (!BashCommands.IsVersionSuitable(pythonMajor, pythonMinorMin, currPythonPath, BashCommands.pythonVersionRegex)) continue;

                    logsProgress?.Report($"Found python at {currPythonPath} from common install locations");
                    return currPythonPath;
                }
            }
            logsProgress?.Report("Python not found in common install locations");
            return null;
        }
    }
}
