using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.Devices
{
	[ApiClass(typeof(ProxyDisplayWithAudio))]
	public interface IDisplayWithAudio : IDisplay
	{
		event EventHandler<FloatEventArgs> OnVolumeChanged;

		event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#region Properties

		/// <summary>
		/// Gets the current volume.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_VOLUME, DisplayWithAudioApi.HELP_PROPERTY_VOLUME)]
		float Volume { get; }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_IS_MUTED, DisplayWithAudioApi.HELP_PROPERTY_IS_MUTED)]
		bool IsMuted { get; }

		/// <summary>
		/// The min volume.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MIN, DisplayWithAudioApi.HELP_PROPERTY_VOLUME_DEVICE_MIN)]
		float VolumeDeviceMin { get; }

		/// <summary>
		/// The max volume.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MAX, DisplayWithAudioApi.HELP_PROPERTY_VOLUME_DEVICE_MAX)]
		float VolumeDeviceMax { get; }

		/// <summary>
		/// Prevents the device from going below this volume.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_VOLUME_SAFETY_MIN, DisplayWithAudioApi.HELP_PROPERTY_VOLUME_SAFETY_MIN)]
		float? VolumeSafetyMin { get; set; }

		/// <summary>
		/// Prevents the device from going above this volume.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_VOLUME_SAFETY_MAX, DisplayWithAudioApi.HELP_PROPERTY_VOLUME_SAFETY_MAX)]
		float? VolumeSafetyMax { get; set; }

		/// <summary>
		/// The volume the device is set to when powered.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_VOLUME_DEFAULT, DisplayWithAudioApi.HELP_PROPERTY_VOLUME_DEFAULT)]
		float? VolumeDefault { get; set; }

		#endregion

		#region Methods

		[ApiMethod(DisplayWithAudioApi.METHOD_SET_VOLUME, DisplayWithAudioApi.HELP_METHOD_SET_VOLUME)]
		void SetVolume(float raw);

		[ApiMethod(DisplayWithAudioApi.METHOD_VOLUME_UP_INCREMENT, DisplayWithAudioApi.HELP_METHOD_VOLUME_UP_INCREMENT)]
		void VolumeUpIncrement();

		[ApiMethod(DisplayWithAudioApi.METHOD_VOLUME_DOWN_INCREMENT, DisplayWithAudioApi.HELP_METHOD_VOLUME_DOWN_INCREMENT)]
		void VolumeDownIncrement();

		[ApiMethod(DisplayWithAudioApi.METHOD_MUTE_ON, DisplayWithAudioApi.HELP_METHOD_MUTE_ON)]
		void MuteOn();

		[ApiMethod(DisplayWithAudioApi.METHOD_MUTE_OFF, DisplayWithAudioApi.HELP_METHOD_MUTE_OFF)]
		void MuteOff();

		[ApiMethod(DisplayWithAudioApi.METHOD_MUTE_TOGGLE, DisplayWithAudioApi.HELP_METHOD_MUTE_TOGGLE)]
		void MuteToggle();

		#endregion
	}

	/// <summary>
	/// Extension methods for IDisplayWithAudio devices.
	/// </summary>
	public static class DisplayWithAudioExtensions
	{
		/// <summary>
		/// Returns the safety min if set, otherwise returns the device min.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static float GetVolumeSafetyOrDeviceMin(this IDisplayWithAudio extends)
		{
			return extends.VolumeSafetyMin ?? extends.VolumeDeviceMin;
		}

		/// <summary>
		/// Returns the safety max if set, otherwise returns the device max.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static float GetVolumeSafetyOrDeviceMax(this IDisplayWithAudio extends)
		{
			return extends.VolumeSafetyMax ?? extends.VolumeDeviceMax;
		}

		/// <summary>
		/// Sets the volume as a percentage 0.0 - 1.0.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="percentage"></param>
		public static void SetVolumeAsPercentage(this IDisplayWithAudio extends, float percentage)
		{
			float raw = MathUtils.MapRange(0.0f, 1.0f, extends.VolumeDeviceMin, extends.VolumeDeviceMax, percentage);
			extends.SetVolume(raw);
		}

		/// <summary>
		/// Gets the volume percentage of the device 0.0 - 1.0.
		/// </summary>
		public static float GetVolumeAsPercentage(this IDisplayWithAudio extends)
		{
			return GetVolumeAsPercentage(extends.Volume, extends.VolumeDeviceMin, extends.VolumeDeviceMax);
		}

		/// <summary>
		/// Gets the volume percentage of the device 0.0 - 1.0, from safety min to safety max.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static float GetVolumeAsSafetyPercentage(this IDisplayWithAudio extends)
		{
			return GetVolumeAsPercentage(extends.Volume, extends.GetVolumeSafetyOrDeviceMin(),
			                             extends.GetVolumeSafetyOrDeviceMax());
		}

		/// <summary>
		/// Gets the volume percentage of the device 0.0 - 1.0.
		/// </summary>
		public static float GetVolumeAsPercentage(float volume, float min, float max)
		{
			return MathUtils.MapRange(min, max, 0.0f, 1.0f, volume);
		}
	}
}
