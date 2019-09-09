using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
    public sealed class DisplayVolumeControlAvaliableApiEventArgs : AbstractGenericApiEventArgs<bool>
    {
	    /// <summary>
	    /// Constructor.
	    /// </summary>
	    /// <param name="eventName"></param>
	    /// <param name="data"></param>
	    public DisplayVolumeControlAvaliableApiEventArgs(bool data) : base(DisplayWithAudioApi.EVENT_VOLUME_CONTROL_AVALIABLE, data)
	    {
	    }
    }
}
