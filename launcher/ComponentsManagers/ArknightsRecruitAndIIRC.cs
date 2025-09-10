using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcher.ComponentsManagers
{
    internal class ArknightsRecruitAndIIRC : ComponentManager
    {
        internal static readonly ArknightsRecruitAndIIRC instance = new();
        internal const string arknightsRecruitDir = "arknights recruitment";
        internal static readonly string arknightsRecruitDirPath = Path.Join(appDir, arknightsRecruitDir);

        protected override ComponentManager[] Corequisites => corequisitesField;
        protected override int NbSubSteps => 4;
        protected override double Weight => 1;
        protected override ComponentManager[] Prerequisites => emptyPrerequisites;

        private static readonly ComponentManager[] corequisitesField = [Supervisor.instance, Adb.instance];

        private const string recruitCodeUrl = "https://github.com/Greg0733/win_automate_cs/raw/main/arknights recruitment.zip";
        private const string shortcutName = "arknights auto-recruit";
        private static readonly string shortcutPath = Path.Join(userShortcutsDir, $"{shortcutName}.lnk");

        private ArknightsRecruitAndIIRC() { }

        internal override bool CheckInstalled(IProgress<string>? logsProgress = null)
        {
            // TODO could do better
            return Directory.Exists(arknightsRecruitDirPath) && File.Exists(shortcutPath);
        }
        internal override void StartConfiguration()
        {
            new ArknightsRecruit.ConfigureWindow(arknightsRecruitDirPath).Activate();
        }
        internal override void StartComponent()
        {
            BashCommands.RunShortcut(shortcutPath);
        }

        protected override async Task ComponentInstallAsync()
        {
            await Utils.DownloadAndExtractAsync(recruitCodeUrl, arknightsRecruitDirPath, new Progress<string>((s) => ReportInstallProgress($"{s} {shortcutName} code")));

            ReportInstallProgress($"Creating shortcut named {shortcutName}");
            // TODO setup Windows script host object model and try with new WshShell
            BashCommands.RunExe("powershell.exe", $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{shortcutPath}');$s.TargetPath='{PythonVenv.pythonVenvPath}';$s.WorkingDirectory='{appDir}';$s.Arguments='start_bot.py \\\"{arknightsRecruitDir}\\\"';$s.WindowStyle=7;$s.Save();");

            ReportInstallProgress($"Installed {shortcutName}", true);
        }
    }
}
