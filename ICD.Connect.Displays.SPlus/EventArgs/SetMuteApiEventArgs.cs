using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.EventArgs
{
	public sealed class SetMuteApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetMuteApiEventArgs(bool data) : base(SPlusDisplayApi.EVENT_SET_MUTE, data)
		{
		}
	}
}