using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Proxies;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Proxies
{
	public abstract class AbstractProxyDisplay : AbstractProxyDevice, IProxyDisplay
	{
		public event EventHandler<BoolEventArgs> OnIsPoweredChanged;

		public event DisplayHdmiInputDelegate OnHdmiInputChanged;

		public event EventHandler<ScalingModeEventArgs> OnScalingModeChanged;

		private bool m_IsPowered;
		private int? m_HdmiInput;
		private eScalingMode m_ScalingMode;

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public bool IsPowered
		{
			get { return m_IsPowered; }
			[UsedImplicitly]
			private set
			{
				if (value == m_IsPowered)
					return;

				m_IsPowered = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - Power set to {1}", this, m_IsPowered);

				OnIsPoweredChanged.Raise(this, new BoolEventArgs(m_IsPowered));
			}
		}

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		public int InputCount { get; [UsedImplicitly] private set; }

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		public int? HdmiInput
		{
			get { return m_HdmiInput; }
			[UsedImplicitly]
			private set
			{
				if (value == m_HdmiInput)
					return;

				int? oldInput = m_HdmiInput;
				m_HdmiInput = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - Hdmi input set to {1}", this, m_HdmiInput);

				DisplayHdmiInputDelegate handler = OnHdmiInputChanged;
				if (handler == null)
					return;

				if (oldInput.HasValue)
					handler(this, oldInput.Value, false);

				if (m_HdmiInput.HasValue)
					handler(this, m_HdmiInput.Value, true);
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

				OnScalingModeChanged.Raise(this, new ScalingModeEventArgs(m_ScalingMode));
			}
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public void PowerOn()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public void PowerOff()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public void SetHdmiInput(int address)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public void SetScalingMode(eScalingMode mode)
		{
			throw new NotImplementedException();
		}

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
