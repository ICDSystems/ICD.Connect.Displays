using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Mock.Devices
{
	/// <summary>
	/// Settings for the MockDisplayWithAudio device.
	/// </summary>
	[KrangSettings("MockDisplayWithAudio", typeof(MockDisplayWithAudio))]
	public sealed class MockDisplayWithAudioSettings : AbstractDisplayWithAudioSettings
	{
	}
}
