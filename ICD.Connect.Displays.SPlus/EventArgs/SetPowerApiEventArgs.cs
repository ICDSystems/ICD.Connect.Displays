using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.EventArgs
{
	public sealed class SetPowerApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetPowerApiEventArgs(bool data) : base(SPlusDisplayApi.EVENT_SET_POWER, data)
		{
		}
	}
}