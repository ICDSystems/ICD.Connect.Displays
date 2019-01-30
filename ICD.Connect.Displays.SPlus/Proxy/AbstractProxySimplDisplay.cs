using System;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.SPlus.Devices.Simpl;
using ICD.Connect.Displays.SPlus.EventArgs;

namespace ICD.Connect.Displays.SPlus.Proxy
{
	public abstract class AbstractProxySimplDisplay<TSettings> : AbstractSimplProxyDevice<TSettings>, ISimplDisplay
		where TSettings : IProxySimplDisplaySettings
	{
		public event EventHandler<SetPowerApiEventArgs> OnSetPower;
		public event EventHandler<SetActiveInputApiEventArgs> OnSetActiveInput;
		public event EventHandler<SetScalingModeEventArgs> OnSetScalingMode;
		public void SetPowerFeedback(bool isPowered)
		{
			throw new NotImplementedException();
		}

		public void SetActiveInputFeedback(int? address)
		{
			throw new NotImplementedException();
		}

		public void SetScalingModeFeedback(eScalingMode mode)
		{
			throw new NotImplementedException();
		}
	}
}