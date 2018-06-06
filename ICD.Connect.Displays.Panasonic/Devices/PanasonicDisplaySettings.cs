using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Panasonic.Devices
{
	[KrangSettings("PanasonicDisplay", typeof(PanasonicDisplay))]
	public sealed class PanasonicDisplaySettings : AbstractDisplayWithAudioSettings
	{
	}
}
