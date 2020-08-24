using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices.CrestronSPlus.Devices.SPlus;
using ICD.Connect.Displays.SPlus.EventArgs;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public interface ISimplDisplay : ISPlusDevice
	{

		[ApiEvent(SPlusDisplayApi.EVENT_SET_POWER, SPlusDisplayApi.EVENT_SET_POWER_HELP)]
		event EventHandler<SetPowerApiEventArgs> OnSetPower;

		[ApiEvent(SPlusDisplayApi.EVENT_SET_ACTIVE_INPUT, SPlusDisplayApi.EVENT_SET_ACTIVE_INPUT_HELP)]
		event EventHandler<SetActiveInputApiEventArgs> OnSetActiveInput;

		[ApiMethod(SPlusDisplayApi.METHOD_SET_POWER_FEEDBACK, SPlusDisplayApi.METHOD_SET_POWER_FEEDBACK_HELP)]
		void SetPowerFeedback(bool isPowered);

		[ApiMethod(SPlusDisplayApi.METHOD_SET_ACTIVE_INPUT_FEEDBACK, SPlusDisplayApi.METHOD_SET_ACTIVE_INPUT_FEEDBACK_HELP)]
		void SetActiveInputFeedback(int? address);
	}
}
