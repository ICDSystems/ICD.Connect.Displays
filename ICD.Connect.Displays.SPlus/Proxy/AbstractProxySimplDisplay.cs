using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Devices.CrestronSPlus.Devices.SPlus;
using ICD.Connect.Displays.SPlus.Devices.Simpl;
using ICD.Connect.Displays.SPlus.EventArgs;

namespace ICD.Connect.Displays.SPlus.Proxy
{
	public abstract class AbstractProxySimplDisplay<TSettings> : AbstractSPlusProxyDevice<TSettings>, ISimplDisplay
		where TSettings : IProxySimplDisplaySettings
	{
		public event EventHandler<SetPowerApiEventArgs> OnSetPower;
		public event EventHandler<SetActiveInputApiEventArgs> OnSetActiveInput;

		#region Public Methods
		
		public void SetPowerFeedback(bool isPowered)
		{
			CallMethod(SPlusDisplayApi.METHOD_SET_POWER_FEEDBACK, isPowered);
		}

		public void SetActiveInputFeedback(int? address)
		{
			CallMethod(SPlusDisplayApi.METHOD_SET_ACTIVE_INPUT_FEEDBACK, address);
		}

		#endregion

		#region API

		/// <summary>
		/// Override to build initialization commands on top of the current class info.
		/// </summary>
		/// <param name="command"></param>
		protected override void Initialize(ApiClassInfo command)
		{
			base.Initialize(command);

			ApiCommandBuilder.UpdateCommand(command)
			                 .SubscribeEvent(SPlusDisplayApi.EVENT_SET_POWER)
			                 .SubscribeEvent(SPlusDisplayApi.EVENT_SET_ACTIVE_INPUT)
			                 .Complete();
		}

		/// <summary>
		/// Updates the proxy with event feedback info.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		protected override void ParseEvent(string name, ApiResult result)
		{
			base.ParseEvent(name, result);

			switch (name)
			{
				case SPlusDisplayApi.EVENT_SET_POWER:
					bool powerState = result.GetValue<bool>();
					RaiseSetPower(powerState);
					break;
				case SPlusDisplayApi.EVENT_SET_ACTIVE_INPUT:
					int activeInput = result.GetValue<int>();
					RaiseSetActiveInput(activeInput);
					break;
			}
		}

		#endregion

		#region Private Methods

		private void RaiseSetPower(bool powerState)
		{
			OnSetPower.Raise(this, new SetPowerApiEventArgs(powerState));
		}

		private void RaiseSetActiveInput(int activeInput)
		{
			OnSetActiveInput.Raise(this, new SetActiveInputApiEventArgs(activeInput));
		}

		#endregion
	}
}