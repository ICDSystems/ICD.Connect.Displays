using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	[KrangSettings("SamsungDisplay", typeof(SamsungDisplay))]
	public sealed class SamsungDisplaySettings : AbstractDisplayWithAudioSettings
	{
	}
}
