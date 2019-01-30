using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.SPlus.EventArgs;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public interface ISimplDisplay : ISimplDevice
	{

		[ApiEvent(SPlusDisplayApi.EVENT_SET_POWER, SPlusDisplayApi.EVENT_SET_POWER_HELP)]
		event EventHandler<SetPowerApiEventArgs> OnSetPower;

		[ApiEvent(SPlusDisplayApi.EVENT_SET_ACTIVE_INPUT, SPlusDisplayApi.EVENT_SET_ACTIVE_INPUT_HELP)]
		event EventHandler<SetActiveInputApiEventArgs> OnSetActiveInput;

		[ApiEvent(SPlusDisplayApi.EVENT_SET_SCALING_MODE, SPlusDisplayApi.EVENT_SET_SCALING_MODE_HELP)]
		event EventHandler<SetScalingModeEventArgs> OnSetScalingMode;

		[ApiMethod(SPlusDisplayApi.METHOD_SET_POWER_FEEDBACK, SPlusDisplayApi.METHOD_SET_POWER_FEEDBACK_HELP)]
		void SetPowerFeedback(bool isPowered);

		[ApiMethod(SPlusDisplayApi.METHOD_SET_ACTIVE_INPUT_FEEDBACK, SPlusDisplayApi.METHOD_SET_ACTIVE_INPUT_FEEDBACK_HELP)]
		void SetActiveInputFeedback(int? address);

		[ApiMethod(SPlusDisplayApi.METHOD_SET_SCALING_MODE_FEEDBACK, SPlusDisplayApi.METHOD_SET_SCALING_MODE_FEEDBACK_HELP)]
		void SetScalingModeFeedback(eScalingMode mode);
	}
}
