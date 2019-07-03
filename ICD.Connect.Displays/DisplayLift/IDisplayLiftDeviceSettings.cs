using ICD.Connect.Devices;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.DisplayLift
{
    public interface IDisplayLiftDeviceSettings : IDeviceSettings
    {
        [OriginatorIdSettingsProperty(typeof(IDisplay))]
        int? Display { get; set; }
        int? BootDelay { get; set; }
        int? CoolingDelay { get; set; }
    }
}
