using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Displays
{
	public interface IDisplayWithAudio : IDisplay
	{
		event EventHandler<FloatEventArgs> OnVolumeChanged;

		event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#region Properties

		/// <summary>
		/// Gets the current volume.
		/// </summary>
		float Volume { get; }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		bool IsMuted { get; }

		/// <summary>
		/// The min volume.
		/// </summary>
		float VolumeDeviceMin { get; }

		/// <summary>
		/// The max volume.
		/// </summary>
		float VolumeDeviceMax { get; }

		/// <summary>
		/// Prevents the device from going below this volume.
		/// </summary>
		[PublicAPI]
		float? VolumeSafetyMin { get; set; }

		/// <summary>
		/// Prevents the device from going above this volume.
		/// </summary>
		[PublicAPI]
		float? VolumeSafetyMax { get; set; }

		/// <summary>
		/// The volume the device is set to when powered.
		/// </summary>
		[PublicAPI]
		float? VolumeDefault { get; set; }

		#endregion

		#region Methods

		void SetVolume(float raw);

		void VolumeUpIncrement();

		void VolumeDownIncrement();

		void MuteOn();

		void MuteOff();

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
