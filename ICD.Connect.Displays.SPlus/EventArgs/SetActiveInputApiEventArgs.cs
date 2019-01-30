using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.EventArgs
{
	public sealed class SetActiveInputApiEventArgs : AbstractGenericApiEventArgs<int>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetActiveInputApiEventArgs(int data) : base(SPlusDisplayApi.EVENT_SET_ACTIVE_INPUT, data)
		{
		}
	}
}