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
    }
}
