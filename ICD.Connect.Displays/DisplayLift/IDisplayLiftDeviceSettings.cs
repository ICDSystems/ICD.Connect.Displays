using ICD.Connect.Devices;

namespace ICD.Connect.Displays.DisplayLift
{
    public interface IDisplayLiftDeviceSettings : IDeviceSettings
    {
        int? Display { get; set; }
        int? BootDelay { get; set; }
        int? CoolingDelay { get; set; }
    }
}
