using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.SPlus.EventArgs;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public abstract class AbstractSimplDisplay<TSettings> : AbstractSimplDevice<TSettings>, ISimplDisplay, IDisplay
		where TSettings : AbstractSimplDisplaySettings, new()
	{
		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		public event EventHandler<DisplayPowerStateApiEventArgs> OnIsPoweredChanged;

		/// <summary>
		/// Raised when the selected HDMI input changes.
		/// </summary>
		public event EventHandler<DisplayInputApiEventArgs> OnActiveInputChanged;

		/// <summary>
		/// Raised when the scaling mode changes.
		/// </summary>
		public event EventHandler<DisplayScalingModeApiEventArgs> OnScalingModeChanged;

		private bool m_IsPowered;
		private int? m_ActiveInput;
		private eScalingMode m_ScalingMode;

		#region Callbacks

		public event EventHandler<SetPowerApiEventArgs> OnSetPower;

		public event EventHandler<SetActiveInputApiEventArgs> OnSetActiveInput;
		public event EventHandler<SetScalingModeEventArgs> OnSetScalingMode;
		
		public void SetPowerFeedback(bool isPowered)
		{
			IsPowered = isPowered;
		}

		public void SetActiveInputFeedback(int? address)
		{
			ActiveInput = address;
		}

		public void SetScalingModeFeedback(eScalingMode mode)
		{
			ScalingMode = mode;
		}

		#endregion

		#region Properties

		/// <summary>
		/// When true assume TX is successful even if a request times out.
		/// </summary>
		public bool Trust { get; set; }

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public bool IsPowered
		{
			get { return m_IsPowered; }
			private set
			{
				if (value == m_IsPowered)
					return;

				m_IsPowered = value;

				Log(eSeverity.Informational, "Power set to {0}", m_IsPowered);

				OnIsPoweredChanged.Raise(this, new DisplayPowerStateApiEventArgs(m_IsPowered));
			}
		}

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		public int? ActiveInput
		{
			get { return m_ActiveInput; }
			private set
			{
				if (value == m_ActiveInput)
					return;

				int? oldInput = m_ActiveInput;
				m_ActiveInput = value;

				Log(eSeverity.Informational, "Active input set to {0}", m_ActiveInput);

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
			private set
			{
				if (value == m_ScalingMode)
					return;

				m_ScalingMode = value;

				Log(eSeverity.Informational, "Scaling mode set to {0}", StringUtils.NiceName(m_ScalingMode));

				OnScalingModeChanged.Raise(this, new DisplayScalingModeApiEventArgs(m_ScalingMode));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractSimplDisplay()
		{
			Controls.Add(new DisplayRouteDestinationControl(this, 0));
			Controls.Add(new DisplayPowerDeviceControl(this, 1));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnIsPoweredChanged = null;
			OnActiveInputChanged = null;
			OnScalingModeChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public void PowerOn()
		{
			OnSetPower.Raise(this, new SetPowerApiEventArgs(true));

			if (Trust)
				IsPowered = true;
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public void PowerOff()
		{
			OnSetPower.Raise(this, new SetPowerApiEventArgs(false));

			if (Trust)
				IsPowered = false;
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public void SetActiveInput(int address)
		{
			OnSetActiveInput.Raise(this, new SetActiveInputApiEventArgs(address));

			if (Trust)
				ActiveInput = address;
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public void SetScalingMode(eScalingMode mode)
		{
			OnSetScalingMode.Raise(this, new SetScalingModeEventArgs(mode));

			if (Trust)
				ScalingMode = mode;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Trust = Trust;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Trust = false;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Trust = settings.Trust;
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
