using ICD.Connect.Displays.Settings;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public interface ISamsungProDisplaySettings: IDisplayWithAudioSettings
	{
		bool DisableLauncher { get; set; }
	}
}