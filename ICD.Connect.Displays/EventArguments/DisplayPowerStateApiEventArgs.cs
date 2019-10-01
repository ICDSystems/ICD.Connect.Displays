using ICD.Connect.API.EventArguments;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public sealed class DisplayPowerStateApiEventArgs : AbstractGenericApiEventArgs<PowerDeviceControlPowerStateEventData>
	{
		public ePowerState PowerState { get { return Data.PowerState; } }

		public long ExpectedDuration { get { return Data.ExpectedDuration; } }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data"></param>
		public DisplayPowerStateApiEventArgs(ePowerState data)
			: this(data, 0)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="powerState"></param>
		/// <param name="expectedDuration"></param>
		public DisplayPowerStateApiEventArgs(ePowerState powerState, long expectedDuration)
			: base(DisplayApi.EVENT_POWER_STATE, new PowerDeviceControlPowerStateEventData(powerState, expectedDuration))
		{
		}
	}
}
