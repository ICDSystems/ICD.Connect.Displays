using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Microsoft.Devices
{
	[KrangSettings("SurfaceHubDisplay", typeof(SurfaceHubDisplay))]
    public sealed class SurfaceHubDisplaySettings : AbstractDisplayWithAudioSettings
    {
    }
}
