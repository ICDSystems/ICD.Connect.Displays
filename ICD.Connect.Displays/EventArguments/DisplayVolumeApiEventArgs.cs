using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public sealed class DisplayVolumeApiEventArgs : AbstractGenericApiEventArgs<float>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DisplayVolumeApiEventArgs(float data)
			: base(DisplayWithAudioApi.EVENT_VOLUME, data)
		{
		}
	}
}
