using ICD.Connect.Devices;

namespace ICD.Connect.Displays.DisplayLift
{
    public interface IDisplayLiftDevice :  IDevice
    {
        eLiftState LiftState { get; }

        void ExtendLift();

        void RetractLift();
    }
}
