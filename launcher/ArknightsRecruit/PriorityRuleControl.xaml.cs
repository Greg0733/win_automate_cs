using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Text.Json;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace launcher.ArknightsRecruit
{
    public sealed partial class PriorityRuleControl : UserControl
    {
        private readonly JsonArknightsRecruitAndIIRC config;
        private event RoutedEventHandler? RemoveControl;
        internal PriorityRuleControl(JsonArknightsRecruitAndIIRC config, PriorityRule? initValues = null, RoutedEventHandler? RemoveControl = null)
        {
            this.config = config;
            this.RemoveControl = RemoveControl;

            InitializeComponent();

            TypesComboBox.ItemsSource = config.ruleTypes;
            ActionsComboBox.ItemsSource = config.actions;
            if (initValues != null)
            {
                PriorityRule initValuesNotNull = (PriorityRule)initValues;

                // This will call the selection changed event and setup ValuesComboBox's items source
                TypesComboBox.SelectedItem = initValuesNotNull.Type;
                if (TypesComboBox.SelectedItem.Equals(config.ruleTypeRarity))
                {
                    ValuesComboBox.SelectedItem = JsonArknightsRecruitAndIIRC.GetSelectedRarity(initValuesNotNull);
                }
                else
                {
                    ValuesComboBox.SelectedItem = JsonArknightsRecruitAndIIRC.GetSelectedOperator(initValuesNotNull);
                }

                ActionsComboBox.SelectedItem = initValuesNotNull.Action;
                GuaranteedCheckBox.IsChecked = initValuesNotNull.Guaranteed;
            }
        }

        internal PriorityRule? GetCurrentState()
        {
            if (TypesComboBox.SelectedItem is not ValueName<int> typeValueName) return null;
            if (ActionsComboBox.SelectedItem is not ValueName<int> ActionValueName) return null;

            dynamic ruleValue = ValuesComboBox.SelectedItem;
            if (ruleValue == null) return null;

            return new()
            {
                Type = typeValueName.value,
                Value = ruleValue,
                Action = ActionValueName.value,
                // IsChecked can't be null because GuaranteedCheckBox is not three-state
                Guaranteed = (bool)GuaranteedCheckBox.IsChecked!
            };
        }

        private void TypesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypesComboBox.SelectedItem.Equals(config.ruleTypeRarity))
            {
                ValuesComboBox.ItemsSource = config.rarities;
            } else
            {
                ValuesComboBox.ItemsSource = config.operators;
            }
            ValuesComboBox.IsEnabled = true;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveControl?.Invoke(this, e);
        }
    }
}
