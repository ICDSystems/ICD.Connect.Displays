using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Displays.DisplayLift
{
    [ExternalTelemetry("Display Lift Telemetry", typeof(DisplayLiftExternalTelemetryProvider))]
    public interface IDisplayLiftDevice : IDevice
    {
        event EventHandler<LiftStateChangedEventArgs> OnLiftStateChanged;
        event EventHandler<IntEventArgs>              OnBootDelayChanged;
        event EventHandler<IntEventArgs>              OnCoolingDelayChanged;

        [CanBeNull]
        IDisplay Display { get; }

        eLiftState LiftState { get; }

        int BootDelay { get; set; }

        int CoolingDelay { get; set; }

        long BootDelayRemaining { get; }

        long CoolingDelayRemaining { get; }

        void ExtendLift(Action postExtend);

        void RetractLift(Action preRetract);
    }
}
