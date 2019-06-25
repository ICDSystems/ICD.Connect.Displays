using ICD.Connect.Devices;

namespace ICD.Connect.Displays.DisplayLift
{
    public interface IDisplayLiftDevice :  IDevice
    {
        eLiftState LiftState { get; }
        
        int BootDelay { get; set; }
        
        int CoolingDelay { get; set; }

        long BootDelayRemaining { get; }
        
        long CoolingDelayRemaining { get; }

        void ExtendLift();

        void RetractLift();
    }
}
