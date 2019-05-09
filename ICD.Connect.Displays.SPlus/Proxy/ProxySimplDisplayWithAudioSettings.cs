using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.SPlus.Proxy
{
	[KrangSettings("ProxySimplDisplayWithAudio", typeof(ProxySimplDisplayWithAudio))]
	public sealed class ProxySimplDisplayWithAudioSettings : AbstractProxySimplDisplaySettings, IProxySimplDisplayWithAudioSettings
	{
	}
}