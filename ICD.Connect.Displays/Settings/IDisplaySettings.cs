using ICD.Connect.Devices;

namespace ICD.Connect.Displays.Settings
{
	public interface IDisplaySettings : IDeviceSettings
	{
		int? Port { get; set; }

		bool Trust { get; set; }
	}
}
