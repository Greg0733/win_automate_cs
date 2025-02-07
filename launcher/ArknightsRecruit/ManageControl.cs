using launcher.ComponentsManagers;

namespace launcher.ArknightsRecruit
{
    internal class ManageControl : ManageComponentControl
    {
        protected override ComponentManager CompanionManager => ArknightsRecruitAndIIRC.instance;
    }
}
