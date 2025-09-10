using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.ComponentsManagers
{
    internal class Supervisor : ComponentManager
    {
        internal static readonly Supervisor instance = new();
        protected override ComponentManager[] Corequisites => corequisitesField;
        protected override ComponentManager[] Prerequisites => emptyPrerequisites;

        protected override int NbSubSteps => 2;

        protected override double Weight => 1;

        private static readonly ComponentManager[] corequisitesField = [PythonVenv.instance];

        private static readonly string supervisorDir = appDir;
        private const string supervisorCodeUrl = "https://github.com/Greg0733/win_automate_cs/raw/main/supervisor.zip";
        private const string supervisorStarterFile = "start_bot.py";

        private Supervisor() { }

        internal override bool CheckInstalled(IProgress<string>? logsProgress = null)
        {
            // TODO could do better
            return File.Exists(supervisorStarterFile);
        }
        internal override void StartConfiguration() => throw new NotImplementedException();
        internal override void StartComponent() => throw new NotImplementedException();

        protected override async Task ComponentInstallAsync()
        {
            await Utils.DownloadAndExtractAsync(supervisorCodeUrl, supervisorDir, new Progress<string>((s) => ReportInstallProgress($"{s} supervisor code")));
        }
    }
}
