using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace launcher
{
    public abstract partial class ManageComponentControl : UserControl
    {
        protected readonly struct StepProgressData
        {
            public readonly string? message;
            public readonly bool stepDone;
            public readonly double? subProgress;

            public StepProgressData(string? message = null, bool stepDone = false, double? subProgress = null)
            {
                this.message = message;
                this.stepDone = stepDone;
                this.subProgress = subProgress;
            }
        }

        protected abstract ComponentsManagers.ComponentManager CompanionManager { get; }

        public string? IconPath { get; set; }
        public string? Title { get; set; }

        private static readonly DependencyProperty DetailsProperty = DependencyProperty.Register
        (
            nameof(DetailsLogs),
            typeof(string),
            typeof(ManageComponentControl),
            null
        );
        public string DetailsLogs
        {
            get { return (string)GetValue(DetailsProperty); }
            set { SetValue(DetailsProperty, value); }
        }
        public string? DetailsTitle {
            get => "Details for " + Title;
        }

        public event RoutedEventHandler? DetailsOn;
        public event RoutedEventHandler? DetailsOff;

        private static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register
        (
            nameof(ProgressValue),
            typeof(double),
            typeof(ManageComponentControl),
            null
        );

        protected double ProgressValue
        {
            get { return (double)GetValue(ProgressValueProperty); }
            set { SetValue(ProgressValueProperty, value); }
        }

        private FrameworkElement? detailsUI = null;
        private bool currInstallException;

        public ManageComponentControl()
        {
            // default value
            IconPath = Path.Join("Assets", "blue.ico");
            InitializeComponent();
            if (CompanionManager.CheckInstalled())
            {
                VisualStateManager.GoToState(this, Installed.Name, true);
            } else
            {
                VisualStateManager.GoToState(this, NotInstalled.Name, true);
            }
        }

        internal FrameworkElement GetDetailsUI()
        {
            if (detailsUI == null)
            {
                // instantiating a UIElement seems to be forbidden in an async context, so this will remain sync
                TextBlock detailsBlock = new()
                {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    IsTextSelectionEnabled = true,
                    //DataContext = inherited from scrollview
                };
                detailsBlock.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Path = new PropertyPath(nameof(DetailsLogs)),
                    TargetNullValue = "Nothing done yet."
                });

                TextBlock titleBlock = new()
                {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    IsTextSelectionEnabled = true,
                };
                titleBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(DetailsTitle)) });

                StackPanel titleDetailsStack = new()
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 5
                };
                titleDetailsStack.Children.Insert(0, titleBlock);
                titleDetailsStack.Children.Insert(1, detailsBlock);

                detailsUI = new ScrollView()
                {
                    HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
                    Content = titleDetailsStack,
                    DataContext = this
                };
            }

            return detailsUI;
        }

        internal double? GetDetailsWidth()
        {
            return detailsUI?.ActualWidth;
        }

        internal bool OwnsDetailsUI(FrameworkElement checkUI)
        {
            return ReferenceEquals(checkUI.DataContext, this);
        }

        private void LogLine(string line)
        {
            DetailsLogs += $"> {line}\n";
        }

        private void AutoInstall_Clicked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, Installing.Name, true);

            currInstallException = false;

            IProgress<ProgressData> installProgress = new Progress<ProgressData>((data) =>
            {
                if (data.message != null)
                {
                    LogLine(data.message);
                }

                if (data.exception)
                {
                    currInstallException = true;
                    VisualStateManager.GoToState(this, Error.Name, true);
                }

                if (data.finished)
                {
                    if (!currInstallException)
                    {
                        VisualStateManager.GoToState(this, Installed.Name, true);
                    }
                }

                if (data.progress != null)
                {
                    ProgressValue = (double) data.progress;
                }
            });

            CompanionManager.InstallAsync(installProgress);
        }

        private void Configure_Clicked(object sender, RoutedEventArgs e)
        {
            CompanionManager.StartConfiguration();
        }

        private void Launch_Clicked(object sender, RoutedEventArgs e)
        {
            CompanionManager.StartComponent();
        }

        private void Details_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch? detailsToggle = sender as ToggleSwitch;
            if (detailsToggle == null)
            {
                // TODO handle
                return;
            }

            if (detailsToggle.IsOn)
            {
                DetailsOn?.Invoke(this, e);
            } 
            else
            {
                DetailsOff?.Invoke(this, e);
            }
        }
    }
}
