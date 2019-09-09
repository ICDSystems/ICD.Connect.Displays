using ICD.Connect.API.EventArguments;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public sealed class DisplayPowerStateApiEventArgs : AbstractGenericApiEventArgs<ePowerState>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DisplayPowerStateApiEventArgs(ePowerState data)
			: base(DisplayApi.EVENT_POWER_STATE, data)
		{
		}
	}
}
