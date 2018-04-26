using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Samsung
{
	[KrangSettings("SamsungDisplay", typeof(SamsungDisplay))]
	public sealed class SamsungDisplaySettings : AbstractDisplayWithAudioSettings
	{
	}
}
