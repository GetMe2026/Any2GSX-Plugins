using Any2GSX.PluginInterface;

namespace Pmdg737Interface
{
    public static class Pmdg737Settings
    {
        public static readonly PluginSetting EnableExperimentalWrites = new()
        {
            Key = "EnableExperimentalWrites",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting StateLogging = new()
        {
            Key = "StateLogging",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting DoorAutomationEnabled = new()
        {
            Key = "DoorAutomationEnabled",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting IntegratedAirstairAuto = new()
        {
            Key = "IntegratedAirstairAuto",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting RearDoorAuto = new()
        {
            Key = "RearDoorAuto",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting JetwayAutoRetractAirstair = new()
        {
            Key = "JetwayAutoRetractAirstair",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting CargoDoorsAuto = new()
        {
            Key = "CargoDoorsAuto",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting PushbackCloseDoors = new()
        {
            Key = "PushbackCloseDoors",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting AlwaysStairsAtJetway = new()
        {
            Key = "AlwaysStairsAtJetway",
            Type = PluginSettingType.Bool
        };

        public static readonly PluginSetting DiagnosticsRearStair = new()
        {
            Key = "DiagnosticsRearStair",
            Type = PluginSettingType.Bool
        };
    }
}
