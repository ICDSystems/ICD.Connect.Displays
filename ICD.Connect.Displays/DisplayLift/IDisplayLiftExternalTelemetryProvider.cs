using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Telemetry;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Nodes.External;

namespace ICD.Connect.Displays.DisplayLift
{
	public interface IDisplayLiftExternalTelemetryProvider : IExternalTelemetryProvider
	{
		[EventTelemetry(DisplayLiftTelemetryNames.LIFT_STATE_CHANGED)]
		event EventHandler<StringEventArgs> OnLiftStateChanged;

		[EventTelemetry(DisplayLiftTelemetryNames.BOOT_DELAY_CHANGED)]
		event EventHandler<StringEventArgs> OnBootDelayChanged;

		[EventTelemetry(DisplayLiftTelemetryNames.COOL_DELAY_CHANGED)]
		event EventHandler<StringEventArgs> OnCoolingDelayChanged;

		[PropertyTelemetry(DisplayLiftTelemetryNames.LIFT_STATE, null, DisplayLiftTelemetryNames.LIFT_STATE_CHANGED)]
		string LiftState { get; }

		[PropertyTelemetry(DisplayLiftTelemetryNames.BOOT_DELAY, null, DisplayLiftTelemetryNames.BOOT_DELAY_CHANGED)]
		string BootDelay { get; }

		[PropertyTelemetry(DisplayLiftTelemetryNames.COOL_DELAY, null, DisplayLiftTelemetryNames.COOL_DELAY_CHANGED)]
		string CoolingDelay { get; }
	}
}
