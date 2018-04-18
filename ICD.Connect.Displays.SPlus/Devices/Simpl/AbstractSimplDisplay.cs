using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public delegate void SimplDisplayPowerOnCallback(IDisplay sender);

	public delegate void SimplDisplayPowerOffCallback(IDisplay sender);

	public delegate void SimplDisplaySetHdmiInputCallback(IDisplay sender, int address);

	public delegate void SimplDisplaySetScalingModeCallback(IDisplay sender, eScalingMode scalingMode);

	public abstract class AbstractSimplDisplay<TSettings> : AbstractSimplDevice<TSettings>, IDisplay
		where TSettings : AbstractSimplDisplaySettings, new()
	{
		private bool m_IsPowered;
		private int? m_HdmiInput;
		private eScalingMode m_ScalingMode;

		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnIsPoweredChanged;

		/// <summary>
		/// Raised when the selected HDMI input changes.
		/// </summary>
		public event DisplayHdmiInputDelegate OnHdmiInputChanged;

		/// <summary>
		/// Raised when the scaling mode changes.
		/// </summary>
		public event EventHandler<ScalingModeEventArgs> OnScalingModeChanged;

		#region Callbacks

		public SimplDisplayPowerOnCallback PowerOnCallback { get; set; }

		public SimplDisplayPowerOffCallback PowerOffCallback{ get; set; }

		public SimplDisplaySetHdmiInputCallback SetHdmiInputCallback{ get; set; }

		public SimplDisplaySetScalingModeCallback SetScalingModeCallback{ get; set; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public bool IsPowered
		{
			get { return m_IsPowered; }
			set
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
		public int InputCount { get; set; }

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		public int? HdmiInput
		{
			get { return m_HdmiInput; }
			set
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
			set
			{
				if (value == m_ScalingMode)
					return;

				m_ScalingMode = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - Scaling mode set to {1}", this, StringUtils.NiceName(m_ScalingMode));

				OnScalingModeChanged.Raise(this, new ScalingModeEventArgs(m_ScalingMode));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractSimplDisplay()
		{
			Controls.Add(new DisplayRouteDestinationControl(this, 0));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnIsPoweredChanged = null;
			OnHdmiInputChanged = null;
			OnScalingModeChanged = null;

			PowerOnCallback = null;
			PowerOffCallback = null;
			SetHdmiInputCallback = null;
			SetScalingModeCallback = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public void PowerOn()
		{
			SimplDisplayPowerOnCallback handler = PowerOnCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public void PowerOff()
		{
			SimplDisplayPowerOffCallback handler = PowerOffCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public void SetHdmiInput(int address)
		{
			SimplDisplaySetHdmiInputCallback handler = SetHdmiInputCallback;
			if (handler != null)
				handler(this, address);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public void SetScalingMode(eScalingMode mode)
		{
			SimplDisplaySetScalingModeCallback handler = SetScalingModeCallback;
			if (handler != null)
				handler(this, mode);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Powered", IsPowered);
			addRow("Hdmi Input", HdmiInput);
			addRow("Scaling Mode", ScalingMode);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("PowerOn", "Turns on the display", () => PowerOn());
			yield return new ConsoleCommand("PowerOff", "Turns off the display", () => PowerOff());

			string hdmiRange = StringUtils.RangeFormat(1, InputCount);
			yield return new GenericConsoleCommand<int>("SetHdmiInput", "SetHdmiInput x " + hdmiRange, i => SetHdmiInput(i));

			yield return new EnumConsoleCommand<eScalingMode>("SetScalingMode", a => SetScalingMode(a));
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
