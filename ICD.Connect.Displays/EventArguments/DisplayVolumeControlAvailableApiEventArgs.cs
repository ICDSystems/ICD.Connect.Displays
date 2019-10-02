using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public sealed class DisplayVolumeControlAvailableApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DisplayVolumeControlAvailableApiEventArgs(bool data)
			: base(DisplayWithAudioApi.EVENT_VOLUME_CONTROL_AVAILABLE, data)
		{
		}
	}
}
