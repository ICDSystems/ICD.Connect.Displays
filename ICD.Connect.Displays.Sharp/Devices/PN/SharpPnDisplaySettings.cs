using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sharp.Devices.PN
{
	/// <summary>
	/// Settings for the SharpDisplay device.
	/// </summary>
	[KrangSettings("SharpPnDisplay", typeof(SharpPnDisplay))]
	public sealed class SharpPnDisplaySettings : AbstractDisplayWithAudioSettings
	{
	}
}
