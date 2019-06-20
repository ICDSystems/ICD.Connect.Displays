namespace ICD.Connect.Displays.DisplayLift
{
    public interface IRelayDisplayLiftDeviceSettings : IDisplayLiftDeviceSettings
    {
        int? DisplayExtendRelay  { get; set; }
        int? DisplayRetractRelay { get; set; }
        bool LatchRelay { get; set; }
        int ExtendTime { get; set; }
        int RetractTime { get; set; }
    }
}
