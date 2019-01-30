using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.EventArgs
{
	public sealed class SetScalingModeEventArgs : AbstractGenericApiEventArgs<eScalingMode>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetScalingModeEventArgs(eScalingMode data) : base(SPlusDisplayApi.EVENT_SET_SCALING_MODE, data)
		{
		}
	}
}