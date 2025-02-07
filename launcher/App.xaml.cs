using launcher.ComponentsManagers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace launcher
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static readonly string appName = "Win automate launcher";

		private Window? m_window;
		private static readonly Color titleBarBackground = Color.FromArgb(0xFF, 0x00, 0xA0, 0xB0);
		private static readonly Color titleBarForeground = Color.FromArgb(0xFF, 0xFB, 0xFB, 0xFB);

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                string testDirPath = Path.Join(ComponentManager.appDir, "test_privileges");
                Directory.CreateDirectory(testDirPath);
                Directory.Delete(testDirPath);
            } catch (UnauthorizedAccessException)
            {
                BashCommands.RunExe(Path.Join(ComponentManager.appDir, $"{appName}.exe"), "", admin:true, waitForExit: false);
                Exit();
                return;
            }
            m_window = new MainWindow();
            //m_window = new ConfigureArknightsRecruitAndIIRC(ManageArknightsRecruitAndIIRC.arknightsRecruitDir);
            m_window.Activate();
        }

        public static void ConfigureAppWindow(AppWindow appWindow, int? width = null)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                AppWindowTitleBar mainTitleBar = appWindow.TitleBar;
				mainTitleBar.BackgroundColor = titleBarBackground;
				mainTitleBar.InactiveBackgroundColor = titleBarBackground;
                mainTitleBar.ButtonBackgroundColor = titleBarBackground;
                mainTitleBar.ButtonInactiveBackgroundColor = titleBarBackground;

                mainTitleBar.ForegroundColor = titleBarForeground;
                mainTitleBar.InactiveForegroundColor = titleBarForeground;
                mainTitleBar.ButtonForegroundColor = titleBarForeground;
                mainTitleBar.ButtonInactiveForegroundColor = titleBarForeground;
			}
            appWindow.SetIcon("Assets/heart.ico");
            if (width != null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32
                {
                    Width = (int)width,
                    Height = (int)(width * 9 / 16)
                });
            }
        }
    }
}
