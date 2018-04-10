using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public enum eScalingMode
	{
		Unknown = 0,
		Wide16X9 = 1,
		Square4X3 = 2,
		NoScale = 3,
		Zoom = 4
	}

	public sealed class DisplayScalingModeApiEventArgs : AbstractGenericApiEventArgs<eScalingMode>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DisplayScalingModeApiEventArgs(eScalingMode data)
			: base(DisplayApi.EVENT_SCALING_MODE, data)
		{
		}
	}
}
