using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace launcher.ComponentsManagers
{
    internal partial class PythonVenv : ComponentManager
    {
        internal static readonly PythonVenv instance = new();

        internal static readonly string pythonVenvDir = Path.Join(toolsDir, "Python");
        internal static readonly string pythonVenvPath = Path.Join(pythonVenvDir, "Scripts", Python.pythonExeName);
        protected override ComponentManager[] Corequisites => emptyPrerequisites;
        protected override ComponentManager[] Prerequisites => prerequisitesField;

        // 82 lines + 20% for safeguard and installation (only counting downloads currently)
        protected override int NbSubSteps => 105;

        protected override double Weight => 5;

        private static readonly ComponentManager[] prerequisitesField = [Python.instance, Supervisor.instance];
        private const string requirementsPath = "hand_req.txt";
        //private static readonly Regex pipDownloadPercentRegex = new(@"([0-9]+(?:\.[0-9]+)?)/([0-9]+(?:\.[0-9]+)?)");
        private static readonly Regex pipDownloadModuleRegex = pdmCompileRegex();

        private PythonVenv() { }

        internal override bool CheckInstalled(IProgress<string>? logsProgress = null)
        {
            // TODO could do better
            return File.Exists(pythonVenvPath);
        }
        internal override void StartConfiguration() => throw new NotImplementedException();
        internal override void StartComponent() => throw new NotImplementedException();

        protected override async Task ComponentInstallAsync()
        {
            await Task.Run(() =>
            {
                string pythonPath = Python.instance.GetPython();

                ReportInstallProgress("Creating python venv");
                BashCommands.RunExe(pythonPath, $"-m venv \"{pythonVenvDir}\"");

                ReportInstallProgress("Installing python libraries");
                BashCommands.MonitorExeOutput(pythonVenvPath, $"-m pip install -r \"{requirementsPath}\"", GetMonitorPipProgress());

                ReportInstallProgress("Python virtual environment created", true);
            });
            
        }

        private Progress<string> GetMonitorPipProgress()
        {
            return new Progress<string>(installLine =>
            {
                if (pipDownloadModuleRegex.Match(installLine).Success)
                {
                    ReportInstallProgress(installLine);
                }
                /* TODO create a pseudo console (conPty) to capture progress stream https://github.com/microsoft/terminal/issues/251
                else
                {
                    Match percentMatch = pipDownloadPercentRegex.Match(installLine);
                    if (percentMatch.Success)
                    {
                        double num = double.Parse(percentMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                        double den = double.Parse(percentMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                        detailsProgress.Report(new(installLine, subProgress: num / den));
                    }
                }
                */
            });
        }

		[GeneratedRegex(@"(?:using cached|downloading|requirement already satisfied)", RegexOptions.IgnoreCase, "fr-BE")]
		private static partial Regex pdmCompileRegex();
	}
}
