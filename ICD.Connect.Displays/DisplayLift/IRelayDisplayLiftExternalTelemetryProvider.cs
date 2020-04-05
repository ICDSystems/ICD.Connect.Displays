using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Telemetry;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Displays.DisplayLift
{
	public interface IRelayDisplayLiftExternalTelemetryProvider : IExternalTelemetryProvider
	{
		[EventTelemetry(DisplayLiftTelemetryNames.LATCH_MODE_CHANGED)]
		event EventHandler<BoolEventArgs> OnLatchModeChanged;

		[EventTelemetry(DisplayLiftTelemetryNames.EXTEND_TIME_CHANGED)]
		event EventHandler<StringEventArgs> OnExtendTimeChanged;

		[EventTelemetry(DisplayLiftTelemetryNames.RETRACT_TIME_CHANGED)]
		event EventHandler<StringEventArgs> OnRetractTimeChanged;

		[DynamicPropertyTelemetry(DisplayLiftTelemetryNames.LATCH_MODE, null, DisplayLiftTelemetryNames.LATCH_MODE_CHANGED)]
		bool LatchMode { get; }

		[DynamicPropertyTelemetry(DisplayLiftTelemetryNames.EXTEND_TIME, null, DisplayLiftTelemetryNames.EXTEND_TIME_CHANGED)]
		string ExtendTime { get; }

		[DynamicPropertyTelemetry(DisplayLiftTelemetryNames.RETRACT_TIME, null,
			DisplayLiftTelemetryNames.RETRACT_TIME_CHANGED)]
		string RetractTime { get; }
	}
}
