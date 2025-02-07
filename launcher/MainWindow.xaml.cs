using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace launcher
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly int initWidth = 690;
        public MainWindow()
        {
            InitializeComponent();
            App.ConfigureAppWindow(AppWindow, initWidth);
        }

        // if widthAdd is negative, the width will decrease
        private void ChangeWidth(double widthAdd)
        {
            AppWindow.ResizeClient(new Windows.Graphics.SizeInt32
            {
                Width = (int)(AppWindow.ClientSize.Width + widthAdd),
                Height = AppWindow.ClientSize.Height
            });
        }

        private void ShowDetails(object sender, RoutedEventArgs e)
        {
            ManageComponentControl? showDetailsComponent = sender as ManageComponentControl ?? throw new ArgumentException("Only " + typeof(ManageComponentControl).ToString() + " should use this callback");

            FrameworkElement newDetails = showDetailsComponent.GetDetailsUI();

            int prevNbCols = LayoutGrid.ColumnDefinitions.Count;
            LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition());
            LayoutGrid.Children.Insert(prevNbCols, newDetails);
            Grid.SetRow(newDetails, 0);
            Grid.SetColumn(newDetails, prevNbCols);

            ChangeWidth(LayoutGrid.ActualWidth / prevNbCols);
        }
        private void HideDetails(object sender, RoutedEventArgs e)
        {
            ManageComponentControl? hideDetailsComponent = sender as ManageComponentControl ?? throw new ArgumentException("Only " + typeof(ManageComponentControl).ToString() + " should use this callback");

            int hidePos = -1;
            foreach (FrameworkElement currChild in LayoutGrid.Children.Cast<FrameworkElement>())
            {
                if (hideDetailsComponent.OwnsDetailsUI(currChild))
                {
                    hidePos = Grid.GetColumn(currChild);
                    break;
                }
            }
            // TODO this should not happen but throwing would be too much, log a warning ?
            if (hidePos == -1) return;
            
            int prevNbCols = LayoutGrid.ColumnDefinitions.Count;
            LayoutGrid.ColumnDefinitions.RemoveAt(hidePos);
            LayoutGrid.Children.RemoveAt(hidePos);
            foreach (FrameworkElement currChild in LayoutGrid.Children.Cast<FrameworkElement>())
            {
                int currPrevCol = Grid.GetColumn(currChild);
                if (currPrevCol > hidePos)
                {
                    Grid.SetColumn(currChild, currPrevCol - 1);
                }
            }

            ChangeWidth(- LayoutGrid.ActualWidth / prevNbCols);
        }

        private void ArknightsRecruitManual_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ArknightsISAuto_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ArknightsISManual_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BotFMAuto_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BotFMManual_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
