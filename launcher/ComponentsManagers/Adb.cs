using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.ComponentsManagers
{
    internal class Adb : ComponentManager
    {
        internal static readonly Adb instance = new();
        protected override ComponentManager[] Corequisites => emptyPrerequisites;
        protected override ComponentManager[] Prerequisites => emptyPrerequisites;

        protected override int NbSubSteps => 2;

        protected override double Weight => 1;

        private static readonly string adbDir = Path.Join(appDir, "Tools", "adb");
		private static readonly string adbFile = Path.Join(adbDir, "adb.exe");
		private const string adbCodeUrl = "https://github.com/Greg0733/win_automate_cs/raw/main/adb.zip";

        private Adb() { }

        internal override bool CheckInstalled(IProgress<string>? logsProgress = null)
        {
            // TODO could do better
            return File.Exists(adbFile);
        }
        internal override void StartConfiguration() => throw new NotImplementedException();
        internal override void StartComponent() => throw new NotImplementedException();

        protected override async Task ComponentInstallAsync()
        {
            await Utils.DownloadAndExtractAsync(adbCodeUrl, adbDir, new Progress<string>((s) => ReportInstallProgress($"{s} adb")));
        }
    }
}
