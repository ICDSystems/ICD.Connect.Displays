using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Info;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Proxies.Devices;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Proxies
{
	public abstract class AbstractProxyDisplay<TSettings> : AbstractProxyDevice<TSettings>, IProxyDisplay
		where TSettings : IProxyDisplaySettings
	{
		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		public event EventHandler<DisplayPowerStateApiEventArgs> OnPowerStateChanged;

		/// <summary>
		/// Raised when the selected HDMI input changes.
		/// </summary>
		public event EventHandler<DisplayInputApiEventArgs> OnActiveInputChanged;

		/// <summary>
		/// Raised when the scaling mode changes.
		/// </summary>
		public event EventHandler<DisplayScalingModeApiEventArgs> OnScalingModeChanged;

		private bool m_Trust;
		private ePowerState m_PowerState;
		private int? m_ActiveInput;
		private eScalingMode m_ScalingMode;

		#region Properties

		/// <summary>
		/// When true assume TX is successful even if a request times out.
		/// </summary>
		public bool Trust { get { return m_Trust; } set { SetProperty(DisplayApi.PROPERTY_TRUST, value); } }

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public virtual ePowerState PowerState
		{
			get { return m_PowerState; }
			[UsedImplicitly]
			protected set
			{
				if (value == m_PowerState)
					return;

				m_PowerState = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - Power set to {1}", this, m_PowerState);

				OnPowerStateChanged.Raise(this, new DisplayPowerStateApiEventArgs(m_PowerState));
			}
		}

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		public int? ActiveInput
		{
			get { return m_ActiveInput; }
			[UsedImplicitly]
			private set
			{
				if (value == m_ActiveInput)
					return;

				int? oldInput = m_ActiveInput;
				m_ActiveInput = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - Hdmi input set to {1}", this, m_ActiveInput);

				if (oldInput.HasValue)
					OnActiveInputChanged.Raise(this, new DisplayInputApiEventArgs(oldInput.Value, false));

				if (m_ActiveInput.HasValue)
					OnActiveInputChanged.Raise(this, new DisplayInputApiEventArgs(m_ActiveInput.Value, true));
			}
		}

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		public eScalingMode ScalingMode
		{
			get { return m_ScalingMode; }
			[UsedImplicitly] private set
			{
				if (value == m_ScalingMode)
					return;

				m_ScalingMode = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - Scaling mode set to {1}", this, StringUtils.NiceName(m_ScalingMode));

				OnScalingModeChanged.Raise(this, new DisplayScalingModeApiEventArgs(m_ScalingMode));
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public void PowerOn()
		{
			CallMethod(DisplayApi.METHOD_POWER_ON);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public void PowerOff()
		{
			CallMethod(DisplayApi.METHOD_POWER_OFF);
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public void SetActiveInput(int address)
		{
			CallMethod(DisplayApi.METHOD_SET_ACTIVE_INPUT, address);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public void SetScalingMode(eScalingMode mode)
		{
			CallMethod(DisplayApi.METHOD_SET_SCALING_MODE, mode);
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
			                 .SubscribeEvent(DisplayApi.EVENT_POWER_STATE)
			                 .SubscribeEvent(DisplayApi.EVENT_ACTIVE_INPUT)
			                 .SubscribeEvent(DisplayApi.EVENT_SCALING_MODE)
							 .GetProperty(DisplayApi.PROPERTY_TRUST)
			                 .GetProperty(DisplayApi.PROPERTY_POWER_STATE)
			                 .GetProperty(DisplayApi.PROPERTY_ACTIVE_INPUT)
			                 .GetProperty(DisplayApi.PROPERTY_SCALING_MODE)
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
				case DisplayApi.EVENT_POWER_STATE:
					PowerState = result.GetValue<ePowerState>();
					break;

				case DisplayApi.HELP_EVENT_ACTIVE_INPUT:
					DisplayInputState state = result.GetValue<DisplayInputState>();
					if (state.Active)
						ActiveInput = state.Input;
					else if (state.Input == ActiveInput)
						ActiveInput = null;
					break;

				case DisplayApi.EVENT_SCALING_MODE:
					ScalingMode = result.GetValue<eScalingMode>();
					break;
			}
		}

		/// <summary>
		/// Updates the proxy with a property result.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		protected override void ParseProperty(string name, ApiResult result)
		{
			base.ParseProperty(name, result);

			switch (name)
			{
				case DisplayApi.PROPERTY_TRUST:
					m_Trust = result.GetValue<bool>();
					break;

				case DisplayApi.PROPERTY_POWER_STATE:
					PowerState = result.GetValue<ePowerState>();
					break;

				case DisplayApi.PROPERTY_ACTIVE_INPUT:
					ActiveInput = result.GetValue<int?>();
					break;

				case DisplayApi.PROPERTY_SCALING_MODE:
					ScalingMode = result.GetValue<eScalingMode>();
					break;
			}
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in DisplayConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			DisplayConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in DisplayConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
