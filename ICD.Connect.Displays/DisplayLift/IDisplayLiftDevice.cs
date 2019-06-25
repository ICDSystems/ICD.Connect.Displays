using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Displays.DisplayLift
{
    [ExternalTelemetry("Display Lift Telemetry", typeof(DisplayLiftExternalTelemetryProvider))]
    public interface IDisplayLiftDevice : IDevice
    {
        event EventHandler<LiftStateChangedEventArgs> OnLiftStateChanged;
        event EventHandler<IntEventArgs>              OnBootDelayChanged;
        event EventHandler<IntEventArgs>              OnCoolingDelayChanged;

        eLiftState LiftState { get; }

        int BootDelay { get; set; }

        int CoolingDelay { get; set; }

        long BootDelayRemaining { get; }

        long CoolingDelayRemaining { get; }

        void ExtendLift();

        void RetractLift();
    }
}
