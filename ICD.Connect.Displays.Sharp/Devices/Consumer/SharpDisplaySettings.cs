using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sharp.Devices.Consumer
{
	/// <summary>
	/// Settings for the SharpDisplay device.
	/// </summary>
	[KrangSettings("SharpDisplay", typeof(SharpDisplay))]
	public sealed class SharpDisplaySettings : AbstractSharpConsumerDisplaySettings
	{
	}
}
