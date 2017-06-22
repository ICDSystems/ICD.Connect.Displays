using ICD.Common.EventArguments;

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

	public sealed class ScalingModeEventArgs : GenericEventArgs<eScalingMode>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ScalingModeEventArgs(eScalingMode data) : base(data)
		{
		}
	}
}
