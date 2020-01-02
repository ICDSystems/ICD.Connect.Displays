using System;
using ICD.Common.Utils;
using ICD.Connect.API.Attributes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Proxies;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Displays.Devices
{
	[ApiClass(typeof(ProxyDisplayWithAudio), typeof(IDisplay))]
	[ExternalTelemetry("Display With Audio Telemetry", typeof(DisplayWithAudioExternalTelemetryProvider))]
	public interface IDisplayWithAudio : IDisplay
	{
		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		[ApiEvent(DisplayWithAudioApi.EVENT_VOLUME, DisplayWithAudioApi.HELP_EVENT_VOLUME)]
		event EventHandler<DisplayVolumeApiEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		[ApiEvent(DisplayWithAudioApi.EVENT_IS_MUTED, DisplayWithAudioApi.HELP_EVENT_IS_MUTED)]
		[EventTelemetry(DisplayTelemetryNames.MUTE_STATE_CHANGED)]
		event EventHandler<DisplayMuteApiEventArgs> OnMuteStateChanged;

		/// <summary>
		/// Raised when the volume control availability changes
		/// </summary>
		[ApiEvent(DisplayWithAudioApi.EVENT_VOLUME_CONTROL_AVAILABLE, DisplayWithAudioApi.HELP_EVENT_VOLUME_CONTROL_AVAILABLE)]
		event EventHandler<DisplayVolumeControlAvailableApiEventArgs> OnVolumeControlAvailableChanged;

		#region Properties

		/// <summary>
		/// Returns the features that are supported by this display.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_SUPPORTED_VOLUME_FEATURES, DisplayWithAudioApi.HELP_PROPERTY_SUPPORTED_VOLUME_FEATURES)]
		eVolumeFeatures SupportedVolumeFeatures { get; }

		/// <summary>
		/// Gets the current volume.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_VOLUME, DisplayWithAudioApi.HELP_PROPERTY_VOLUME)]
		float Volume { get; }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_IS_MUTED, DisplayWithAudioApi.HELP_PROPERTY_IS_MUTED)]
		[DynamicPropertyTelemetry(DisplayTelemetryNames.MUTE_STATE, DisplayTelemetryNames.MUTE_STATE_CHANGED)]
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
		/// Indicates if volume control is currently available or not
		/// </summary>
		[ApiProperty(DisplayWithAudioApi.PROPERTY_VOLUME_CONTROL_AVAILABLE, DisplayWithAudioApi.HELP_PROPERTY_VOLUME_CONTROL_AVAILABLE)]
		bool VolumeControlAvailable { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Sets the raw volume.
		/// </summary>
		/// <param name="level"></param>
		[ApiMethod(DisplayWithAudioApi.METHOD_SET_VOLUME, DisplayWithAudioApi.HELP_METHOD_SET_VOLUME)]
		[MethodTelemetry(DisplayTelemetryNames.SET_VOLUME)]
		void SetVolume(float level);

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		[ApiMethod(DisplayWithAudioApi.METHOD_VOLUME_UP_INCREMENT, DisplayWithAudioApi.HELP_METHOD_VOLUME_UP_INCREMENT)]
		void VolumeUpIncrement();

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		[ApiMethod(DisplayWithAudioApi.METHOD_VOLUME_DOWN_INCREMENT, DisplayWithAudioApi.HELP_METHOD_VOLUME_DOWN_INCREMENT)]
		void VolumeDownIncrement();

		/// <summary>
		/// Mutes the display.
		/// </summary>
		[ApiMethod(DisplayWithAudioApi.METHOD_MUTE_ON, DisplayWithAudioApi.HELP_METHOD_MUTE_ON)]
		[MethodTelemetry(DisplayTelemetryNames.MUTE_ON)]
		void MuteOn();

		/// <summary>
		/// Unmutes the display.
		/// </summary>
		[ApiMethod(DisplayWithAudioApi.METHOD_MUTE_OFF, DisplayWithAudioApi.HELP_METHOD_MUTE_OFF)]
		[MethodTelemetry(DisplayTelemetryNames.MUTE_OFF)]
		void MuteOff();

		/// <summary>
		/// Toggles the mute state of the display.
		/// </summary>
		[ApiMethod(DisplayWithAudioApi.METHOD_MUTE_TOGGLE, DisplayWithAudioApi.HELP_METHOD_MUTE_TOGGLE)]
		void MuteToggle();

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		[ApiMethod(DisplayWithAudioApi.METHOD_VOLUME_RAMP, DisplayWithAudioApi.HELP_METHOD_VOLUME_RAMP)]
		void VolumeRamp(bool increment, long timeout);

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		[ApiMethod(DisplayWithAudioApi.METHOD_VOLUME_RAMP_STOP, DisplayWithAudioApi.HELP_METHOD_VOLUME_RAMP_STOP)]
		void VolumeRampStop();

		#endregion
	}

	/// <summary>
	/// Extension methods for IDisplayWithAudio devices.
	/// </summary>
	public static class DisplayWithAudioExtensions
	{
		/// <summary>
		/// Gets the volume percentage of the device 0.0 - 1.0.
		/// </summary>
		public static float GetVolumeAsPercentage(this IDisplayWithAudio extends)
		{
			return MathUtils.ToPercent(extends.VolumeDeviceMin, extends.VolumeDeviceMax, extends.Volume);
		}
	}
}
