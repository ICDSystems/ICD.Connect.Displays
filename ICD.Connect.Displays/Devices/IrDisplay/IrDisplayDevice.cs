﻿using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Devices.IrDisplay
{
	public sealed class IrDisplayDevice : AbstractDevice<IrDisplaySettings>, IDisplay
	{
		public event EventHandler<BoolEventArgs> OnIsPoweredChanged;
		public event DisplayHdmiInputDelegate OnHdmiInputChanged;
		public event EventHandler<ScalingModeEventArgs> OnScalingModeChanged;

		private readonly IrDisplayCommands m_Commands;

		private IIrPort m_Port;
		private bool m_IsPowered;
		private int? m_HdmiInput;
		private eScalingMode m_ScalingMode;

		#region Properties

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

				Logger.AddEntry(eSeverity.Informational, "{0} - Power set to {1}", this, m_IsPowered);

				OnIsPoweredChanged.Raise(this, new BoolEventArgs(m_IsPowered));
			}
		}

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		public int InputCount
		{
			get
			{
				return ServiceProvider.GetService<IRoutingGraph>()
				                      .Connections
				                      .GetChildren()
				                      .Where(c => c.Destination.Device == Id)
				                      .Distinct(c => c.Destination.Address)
				                      .Count();
			}
		}

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		public int? HdmiInput
		{
			get { return m_HdmiInput; }
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
			private set
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
		public IrDisplayDevice()
		{
			m_Commands = new IrDisplayCommands();

			Controls.Add(new DisplayRouteDestinationControl(this, 0));
		}

		#region Methods

		/// <summary>
		/// Sets the port.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetIrPort(IIrPort port)
		{
			if (port == m_Port)
				return;

			Unsubscribe(m_Port);
			m_Port = port;
			Subscribe(m_Port);

			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public void PowerOn()
		{
			if (PressAndRelease(m_Commands.CommandPowerOn))
				IsPowered = true;
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public void PowerOff()
		{
			if (PressAndRelease(m_Commands.CommandPowerOff))
				IsPowered = false;
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public void SetHdmiInput(int address)
		{
			bool result;

			switch (address)
			{
				case 1:
					result = PressAndRelease(m_Commands.CommandHdmi1);
					break;

				case 2:
					result = PressAndRelease(m_Commands.CommandHdmi2);
					break;

				case 3:
					result = PressAndRelease(m_Commands.CommandHdmi3);
					break;

				default:
					throw new ArgumentOutOfRangeException("address");
			}

			if (result)
				HdmiInput = address;
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public void SetScalingMode(eScalingMode mode)
		{
			bool result;

			switch (mode)
			{
				case eScalingMode.Wide16X9:
					result = PressAndRelease(m_Commands.CommandWide);
					break;
				case eScalingMode.Square4X3:
					result = PressAndRelease(m_Commands.CommandSquare);
					break;
				case eScalingMode.NoScale:
					result = PressAndRelease(m_Commands.CommandNoScale);
					break;
				case eScalingMode.Zoom:
					result = PressAndRelease(m_Commands.CommandZoom);
					break;
				default:
					throw new ArgumentOutOfRangeException("mode");
			}

			if (result)
				ScalingMode = mode;
		}

		#endregion

		#region Private Methods

		private bool PressAndRelease(string command)
		{
			if (m_Port == null)
			{
				Logger.AddEntry(eSeverity.Error, "{0} unable to send command - port is null.", this);
				return false;
			}

			m_Port.PressAndRelease(command);
			return true;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(IrDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_Port == null ? (int?)null : m_Port.Id;
			settings.Commands.Update(m_Commands);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetIrPort(null);
			m_Commands.Clear();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(IrDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_Commands.Update(settings.Commands);

			IIrPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as IIrPort;
				if (port == null)
					Logger.AddEntry(eSeverity.Error, "Port {0} is not an IR Port", settings.Port);
			}

			SetIrPort(port);
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Subscribe to the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Subscribe(IIrPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(IIrPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when the port online state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Port != null && m_Port.IsOnline;
		}

		#endregion
	}
}