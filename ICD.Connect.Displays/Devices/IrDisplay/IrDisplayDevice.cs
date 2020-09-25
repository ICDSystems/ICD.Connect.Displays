using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Devices.IrDisplay
{
	public sealed class IrDisplayDevice : AbstractDevice<IrDisplaySettings>, IDisplay
	{
		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		public event EventHandler<DisplayPowerStateApiEventArgs> OnPowerStateChanged;

		/// <summary>
		/// Raised when the selected HDMI input changes.
		/// </summary>
		public event EventHandler<DisplayInputApiEventArgs> OnActiveInputChanged;

		private readonly IrDriverProperties m_IrDriverProperties;

		/// <summary>
		/// When true assume TX is successful even if a request times out.
		/// </summary>
		bool IDisplay.Trust { get; set; }

		private readonly IrDisplayCommands m_Commands;

		private IIrPort m_Port;
		private ePowerState m_PowerState;
		private int? m_ActiveInput;

		#region Properties

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public ePowerState PowerState
		{
			get { return m_PowerState; }
			private set
			{
				if (value == m_PowerState)
					return;

				m_PowerState = value;

				Logger.LogSetTo(eSeverity.Informational, "PowerState", m_PowerState);

				OnPowerStateChanged.Raise(this, new DisplayPowerStateApiEventArgs(m_PowerState));
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

				Logger.LogSetTo(eSeverity.Informational, "ActiveInput", m_ActiveInput);

				if (oldInput.HasValue)
					OnActiveInputChanged.Raise(this, new DisplayInputApiEventArgs(oldInput.Value, false));

				if (m_ActiveInput.HasValue)
					OnActiveInputChanged.Raise(this, new DisplayInputApiEventArgs(m_ActiveInput.Value, true));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public IrDisplayDevice()
		{
			m_IrDriverProperties = new IrDriverProperties();
			m_Commands = new IrDisplayCommands();
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

			ConfigurePort(port);

			Unsubscribe(m_Port);
			m_Port = port;
			Subscribe(m_Port);

			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(IIrPort port)
		{
			// IR
			if (port != null)
				port.ApplyDeviceConfiguration(m_IrDriverProperties);
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public void PowerOn()
		{
			if (PressAndRelease(m_Commands.CommandPowerOn))
				PowerState = ePowerState.PowerOn;
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public void PowerOff()
		{
			if (PressAndRelease(m_Commands.CommandPowerOff))
				PowerState = ePowerState.PowerOff;
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public void SetActiveInput(int address)
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
				ActiveInput = address;
		}

		#endregion

		#region Private Methods

		private bool PressAndRelease(string command)
		{
			if (m_Port == null)
			{
				Logger.Log(eSeverity.Error, "Unable to send command - port is null.");
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

			settings.Copy(m_IrDriverProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetIrPort(null);
			m_Commands.Clear();

			m_IrDriverProperties.ClearIrProperties();
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
			m_IrDriverProperties.Copy(settings);

			IIrPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById((int)settings.Port) as IIrPort;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No IR port with id {0}", settings.Port);
				}
			}

			SetIrPort(port);
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(IrDisplaySettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new DisplayRouteDestinationControl(this, 0));
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
		private void PortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
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
