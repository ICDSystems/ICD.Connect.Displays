using System;
using ICD.Common.Utils.Attributes;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Telemetry;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Displays.Devices
{
	public interface IDisplayWithAudioExternalTelemetryProvider : IExternalTelemetryProvider
	{
		/// <summary>
		/// Raised when the volume percent changes.
		/// </summary>
		[EventTelemetry(DisplayTelemetryNames.VOLUME_PERCENT_CHANGED)]
		event EventHandler<DisplayVolumeApiEventArgs> OnVolumePercentChanged;

		/// <summary>
		/// Gets the volume as a float represented from 0.0f (silent) to 1.0f (as loud as possible)
		/// </summary>
		[DynamicPropertyTelemetry(DisplayTelemetryNames.VOLUME_PERCENT, DisplayTelemetryNames.VOLUME_PERCENT_CHANGED)]
		[Range(0.0f, 1.0f)]
		float VolumePercent { get; }
	}
}