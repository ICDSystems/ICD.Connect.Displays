using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public sealed class DisplayMuteApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DisplayMuteApiEventArgs(bool data)
			: base(DisplayWithAudioApi.EVENT_IS_MUTED, data)
		{
		}
	}
}
