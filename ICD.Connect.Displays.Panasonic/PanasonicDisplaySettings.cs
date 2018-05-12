using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Panasonic
{
	[KrangSettings("PanasonicDisplay", typeof(PanasonicDisplay))]
	public sealed class PanasonicDisplaySettings : AbstractDisplayWithAudioSettings
	{
	}
}
