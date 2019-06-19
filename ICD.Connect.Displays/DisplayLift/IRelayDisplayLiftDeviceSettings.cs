namespace ICD.Connect.Displays.DisplayLift
{
    public interface IRelayDisplayLiftDeviceSettings : IDisplayLiftDeviceSettings
    {
        int? DisplayExtendRelay  { get; set; }
        int? DisplayRetractRelay { get; set; }
        bool? LatchRelay { get; set; }
        int? ExtendRelayHoldTime { get; set; }
        int? RetractRelayHoldTime { get; set; }
    }
}
