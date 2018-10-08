using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sharp.Devices.Commercial
{
	/// <summary>
	/// Settings for the SharpProDisplay device.
	/// </summary>
	[KrangSettings("SharpProDisplay", typeof(SharpProDisplay))]
	public sealed class SharpProDisplaySettings : AbstractDisplayWithAudioSettings
	{
	}
}
