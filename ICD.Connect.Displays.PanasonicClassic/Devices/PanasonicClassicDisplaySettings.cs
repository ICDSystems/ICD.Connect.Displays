using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.PanasonicClassic.Devices
{
	[KrangSettings("PanasonicClassicDisplay", typeof(PanasonicClassicDisplay))]
	public sealed class PanasonicClassicDisplaySettings : AbstractDisplayWithAudioSettings
	{
	}
}
