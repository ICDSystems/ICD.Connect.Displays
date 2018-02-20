using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Displays
{
	public sealed class ProxyDisplayWithAudio : AbstractProxyDisplay, IProxyDisplayWithAudio
	{
		public event EventHandler<FloatEventArgs> OnVolumeChanged;

		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		public float Volume { get; }

		public bool IsMuted { get; }

		public float VolumeDeviceMin { get; }

		public float VolumeDeviceMax { get; }

		public float? VolumeSafetyMin { get; set; }

		public float? VolumeSafetyMax { get; set; }

		public float? VolumeDefault { get; set; }

		public void SetVolume(float raw)
		{
			throw new NotImplementedException();
		}

		public void VolumeUpIncrement()
		{
			throw new NotImplementedException();
		}

		public void VolumeDownIncrement()
		{
			throw new NotImplementedException();
		}

		public void MuteOn()
		{
			throw new NotImplementedException();
		}

		public void MuteOff()
		{
			throw new NotImplementedException();
		}

		public void MuteToggle()
		{
			throw new NotImplementedException();
		}
	}
}
