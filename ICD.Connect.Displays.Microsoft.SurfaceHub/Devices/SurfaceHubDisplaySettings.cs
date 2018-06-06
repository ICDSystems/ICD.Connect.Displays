using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Microsoft.SurfaceHub.Devices
{
	[KrangSettings("SurfaceHubDisplay", typeof(SurfaceHubDisplay))]
    public sealed class SurfaceHubDisplaySettings : AbstractDisplayWithAudioSettings
    {
    }
}
