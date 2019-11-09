using ICD.Connect.Displays.Sharp.Devices.Consumer;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sharp.Devices.Prosumer
{
	/// <summary>
	/// Settings for the SharpProsumerDisplay device.
	/// </summary>
	[KrangSettings("SharpProsumerDisplay", typeof(SharpProsumerDisplay))]
	public sealed class SharpProsumerDisplaySettings : AbstractSharpConsumerDisplaySettings
	{
	}
}
