using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public sealed class DisplayPowerStateApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DisplayPowerStateApiEventArgs(bool data)
			: base(DisplayApi.EVENT_IS_POWERED, data)
		{
		}
	}
}
