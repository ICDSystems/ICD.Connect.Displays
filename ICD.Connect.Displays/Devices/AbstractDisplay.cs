using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Devices
{
	/// <summary>
	/// AbstractDisplay represents the base class for all TV displays.
	/// </summary>
	public abstract class AbstractDisplay<T> : AbstractDevice<T>, IDisplay
		where T : IDisplaySettings, new()
	{
		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		public event EventHandler<DisplayPowerStateApiEventArgs> OnIsPoweredChanged;

		/// <summary>
		/// Raised when the selected HDMI input changes.
		/// </summary>
		public event EventHandler<DisplayHmdiInputApiEventArgs> OnHdmiInputChanged;

		/// <summary>
		/// Raised when the scaling mode changes.
		/// </summary>
		public event EventHandler<DisplayScalingModeApiEventArgs> OnScalingModeChanged;

		private readonly ComSpecProperties m_ComSpecProperties;
		private readonly SecureNetworkProperties m_NetworkProperties;

		private bool m_IsPowered;
		private int? m_HdmiInput;
		private eScalingMode m_ScalingMode;

		#region Properties

		/// <summary>
		/// Gets the com spec properties.
		/// </summary>
		protected IComSpecProperties ComSpecProperties { get { return m_ComSpecProperties; } }

		/// <summary>
		/// Gets the network properties.
		/// </summary>
		protected ISecureNetworkProperties NetworkProperties { get { return m_NetworkProperties; } }

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		public abstract int InputCount { get; }

		/// <summary>
		/// Gets and sets the serial port.
		/// </summary>
		protected virtual ISerialQueue SerialQueue { get; private set; }

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public virtual bool IsPowered
		{
			get { return m_IsPowered; }
			protected set
			{
				if (value == m_IsPowered)
					return;

				m_IsPowered = value;

				Log(eSeverity.Informational, "Power set to {0}", m_IsPowered);

				if (m_IsPowered)
					QueryState();

				OnIsPoweredChanged.Raise(this, new DisplayPowerStateApiEventArgs(m_IsPowered));
			}
		}

		/// <summary>
		/// Gets the current hdmi input address.
		/// </summary>
		public int? HdmiInput
		{
			get { return m_HdmiInput; }
			protected set
			{
				if (value == m_HdmiInput)
					return;

				int? oldInput = m_HdmiInput;
				m_HdmiInput = value;

				Log(eSeverity.Informational, "Hdmi input set to {0}", m_HdmiInput);

				if (oldInput.HasValue)
					OnHdmiInputChanged.Raise(this, new DisplayHmdiInputApiEventArgs(oldInput.Value, false));

				if (m_HdmiInput.HasValue)
					OnHdmiInputChanged.Raise(this, new DisplayHmdiInputApiEventArgs(m_HdmiInput.Value, true));
			}
		}

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		public eScalingMode ScalingMode
		{
			get { return m_ScalingMode; }
			protected set
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
		protected AbstractDisplay()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

			Controls.Add(new DisplayRouteDestinationControl(this, 0));
			Controls.Add(new DisplayPowerDeviceControl(this, 1));
		}

		#region Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		[PublicAPI]
		public abstract void SetPort(ISerialPort port);

		/// <summary>
		/// Queues the command to be sent to the device.
		/// </summary>
		/// <param name="command"></param>
		[PublicAPI]
		public void SendCommand(ISerialData command)
		{
			SerialQueue.Enqueue(command);
		}

		/// <summary>
		/// Queues the command to be sent to the device.
		/// Replaces an existing command if it matches the comparer.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="comparer"></param>
		[PublicAPI]
		public void SendCommand<TData>(TData command, Func<TData, TData, bool> comparer)
			where TData : ISerialData
		{
			SerialQueue.Enqueue(command, comparer);
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public abstract void PowerOn();

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public abstract void PowerOff();

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public abstract void SetHdmiInput(int address);

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public abstract void SetScalingMode(eScalingMode mode);

		/// <summary>
		/// Clears resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnIsPoweredChanged = null;
			OnHdmiInputChanged = null;
			OnScalingModeChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(SerialQueue);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the serial queue for communicating with the physical device.
		/// </summary>
		/// <param name="serialQueue"></param>
		protected void SetSerialQueue(ISerialQueue serialQueue)
		{
			Unsubscribe(SerialQueue);

			if (SerialQueue != null)
				SerialQueue.Dispose();

			SerialQueue = serialQueue;

			Subscribe(SerialQueue);

			UpdateCachedOnlineStatus();

			if (IsOnline)
				QueryState();
		}

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected virtual void QueryState()
		{
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return SerialQueue != null && SerialQueue.Port != null && SerialQueue.Port.IsOnline;
		}

		#endregion

		#region SerialQueue Callbacks

		/// <summary>
		/// Subscribes to the serial queue events.
		/// </summary>
		/// <param name="serialQueue"></param>
		private void Subscribe(ISerialQueue serialQueue)
		{
			if (serialQueue == null)
				return;

			serialQueue.OnSerialResponse += SerialQueueOnSerialResponse;
			serialQueue.OnTimeout += SerialQueueOnTimeout;

			if (serialQueue.Port == null)
				return;

			serialQueue.Port.OnIsOnlineStateChanged += SerialQueueOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribes from the serial queue events.
		/// </summary>
		/// <param name="serialQueue"></param>
		private void Unsubscribe(ISerialQueue serialQueue)
		{
			if (serialQueue == null)
				return;

			serialQueue.OnSerialResponse -= SerialQueueOnSerialResponse;
			serialQueue.OnTimeout -= SerialQueueOnTimeout;

			if (serialQueue.Port == null)
				return;

			serialQueue.Port.OnIsOnlineStateChanged -= SerialQueueOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected abstract void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args);

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected abstract void SerialQueueOnTimeout(object sender, SerialDataEventArgs args);

		/// <summary>
		/// Called when the serial queue online state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SerialQueueOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(T settings)
		{
			base.CopySettingsFinal(settings);

			if (SerialQueue != null && SerialQueue.Port != null)
				settings.Port = SerialQueue.Port.Id;
			else
				settings.Port = null;

			settings.Copy(m_ComSpecProperties);
			settings.Copy(m_NetworkProperties);
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPort(null);

			m_ComSpecProperties.Clear();
			m_NetworkProperties.Clear();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_NetworkProperties.Copy(settings);
			m_ComSpecProperties.Copy(settings);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
			}

			SetPort(port);
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
