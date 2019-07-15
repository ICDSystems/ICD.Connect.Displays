using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Displays.DisplayLift
{
	public sealed class LiftStateChangedEventArgs : GenericEventArgs<eLiftState>
	{
		public LiftStateChangedEventArgs(eLiftState data)
			: base(data)
		{
		}
	}
}
