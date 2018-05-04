using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sharp
{
	/// <summary>
	/// Settings for the SharpDisplay device.
	/// </summary>
	[KrangSettings("SharpDisplay", typeof(SharpDisplay))]
	public sealed class SharpDisplaySettings : AbstractDisplayWithAudioSettings
	{
	}
}
