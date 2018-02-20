using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;

namespace ICD.Connect.Displays
{
	public sealed class ProxyDisplayWithAudio : AbstractProxyDisplay, IProxyDisplayWithAudio
	{
		public event EventHandler<FloatEventArgs> OnVolumeChanged;

		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		[ApiProperty("Volume", "Gets the volume of the display.")]
		public float Volume { get; }

		[ApiProperty("IsMuted", "Gets the muted state of the display.")]
		public bool IsMuted { get; }

		[ApiProperty("VolumeDeviceMin", "Gets the min volume of the display.")]
		public float VolumeDeviceMin { get; }

		[ApiProperty("VolumeDeviceMax", "Gets the max volume of the display.")]
		public float VolumeDeviceMax { get; }

		[ApiProperty("VolumeSafetyMin", "Gets/sets the min safety volume.")]
		public float? VolumeSafetyMin { get; set; }

		[ApiProperty("VolumeSafetyMax", "Gets/sets the max safety volume.")]
		public float? VolumeSafetyMax { get; set; }

		[ApiProperty("VolumeDefault", "Gets/sets the default volume.")]
		public float? VolumeDefault { get; set; }

		[ApiMethod("SetVolume", "Sets the display volume.")]
		public void SetVolume(float raw)
		{
			throw new NotImplementedException();
		}

		[ApiMethod("VolumeUpIncrement", "Increments the display volume.")]
		public void VolumeUpIncrement()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("VolumeDownIncrement", "Decrements the display volume.")]
		public void VolumeDownIncrement()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("MuteOn", "Mutes the display.")]
		public void MuteOn()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("MuteOff", "Unmutes the display.")]
		public void MuteOff()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("MuteToggle", "Toggles the display mute state.")]
		public void MuteToggle()
		{
			throw new NotImplementedException();
		}
	}
}
