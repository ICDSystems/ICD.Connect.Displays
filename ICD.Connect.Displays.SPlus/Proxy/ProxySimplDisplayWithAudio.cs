using System;
using ICD.Connect.Displays.SPlus.Devices.Simpl;
using ICD.Connect.Displays.SPlus.EventArgs;

namespace ICD.Connect.Displays.SPlus.Proxy
{
	public sealed class ProxySimplDisplayWithAudio : AbstractProxySimplDisplay<ProxySimplDisplayWithAudioSettings>, ISimplDisplayWithAudio
	{
		public event EventHandler<SetVolumeApiEventArgs> OnSetVolume;
		public event EventHandler<SetVolumeIncrementApiEventArgs> OnSetVolumeIncrement;
		public event EventHandler<SetMuteApiEventArgs> OnSetMute;
		public event EventHandler<SetMuteToggleApiEventArgs> OnSetMuteToggle;
		public void SetVolumeFeedback(float volume)
		{
			throw new NotImplementedException();
		}

		public void SetMuteFeedback(bool mute)
		{
			throw new NotImplementedException();
		}
	}
}