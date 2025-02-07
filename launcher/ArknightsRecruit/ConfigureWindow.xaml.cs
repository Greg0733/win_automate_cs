using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace launcher.ArknightsRecruit
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ConfigureWindow : Window
    {
        private static readonly int initWidth = 1000;
        private readonly JsonArknightsRecruitAndIIRC jsonConfig;
        public ObservableCollection<PriorityRuleControl> priorityRules = [];

        public ConfigureWindow(string configDir)
        {
            // TODO async ?
            jsonConfig = new(configDir);

            InitializeComponent();

			//RefreshToggle.Resources.Add(new("ToggleSwitchFillOn", Color.FromArgb(0xFF, 0xFF, 0xE3, 0x61)));

			FillDefaults();
            
            App.ConfigureAppWindow(AppWindow, initWidth);
        }

        private void FillDefaults()
        {
            Preferences prevPreferences = jsonConfig.preferences;
            TitleTextBox.Text = prevPreferences.WindowTitleContains;
            AskBeforeActingToggle.IsOn = prevPreferences.AskBeforeProceeding ?? false;
            RefreshToggle.IsOn = prevPreferences.DoRefresh ?? false;
            RecruitToggle.IsOn = prevPreferences.DoRecruit ?? false;
            ExpediteToggle.IsOn = prevPreferences.DoExpedite ?? false;
            HireToggle.IsOn = prevPreferences.DoHire ?? false;

            List<PriorityRule>? prevRules = jsonConfig.preferences.PriorityRules;
            if (prevRules == null) return;
            foreach (PriorityRule initValues in prevRules)
            {
                AddRule(initValues);
            }
        }

        private void AddRule(PriorityRule? initValues = null)
        {
            priorityRules.Add(new PriorityRuleControl(jsonConfig, initValues, RemoveRule_Clicked));
        }

        private void NewRule_Clicked(object sender, RoutedEventArgs e)
        {
            AddRule();
        }

        private void RemoveRule_Clicked(object sender, RoutedEventArgs e)
        {
            PriorityRuleControl priorityRuleControl = sender as PriorityRuleControl ?? throw new ArgumentException("Only " + typeof(PriorityRuleControl).ToString() + " should use this callback");

            priorityRules.Remove(priorityRuleControl);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            priorityRules.Clear();
            FillDefaults();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            List<PriorityRule> newRules = new();
            foreach(PriorityRuleControl priorityRuleControl in priorityRules)
            {
                PriorityRule? currRule = priorityRuleControl.GetCurrentState();
                if (currRule == null) continue;

                newRules.Add(currRule.Value);
            }

            jsonConfig.SavePreferences(new()
            {
                WindowTitleContains = TitleTextBox.Text,
                AskBeforeProceeding = AskBeforeActingToggle.IsOn,
                DoRefresh = RefreshToggle.IsOn,
                DoRecruit = RecruitToggle.IsOn,
                DoExpedite = ExpediteToggle.IsOn,
                DoHire = HireToggle.IsOn,
                PriorityRules = newRules
            });

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
