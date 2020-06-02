using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Logging.LoggingContexts;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Routing.Connections;

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
		public event EventHandler<DisplayPowerStateApiEventArgs> OnPowerStateChanged;

		/// <summary>
		/// Raised when the selected HDMI input changes.
		/// </summary>
		public event EventHandler<DisplayInputApiEventArgs> OnActiveInputChanged;

		private readonly ComSpecProperties m_ComSpecProperties;
		private readonly SecureNetworkProperties m_NetworkProperties;

		private readonly ConnectionStateManager m_ConnectionStateManager;

		private ePowerState m_PowerState;
		private int? m_ActiveInput;

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
		/// When true assume TX is successful even if a request times out.
		/// </summary>
		public bool Trust { get; set; }

		/// <summary>
		/// Gets the connection state manager instance.
		/// </summary>
		protected ConnectionStateManager ConnectionStateManager { get { return m_ConnectionStateManager; } }

		/// <summary>
		/// Gets and sets the serial port.
		/// </summary>
		[CanBeNull]
		protected ISerialQueue SerialQueue { get; private set; }

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public virtual ePowerState PowerState
		{
			get { return m_PowerState; }
			protected set
			{
				if (value == m_PowerState)
					return;

				m_PowerState = value;

				Logger.LogSetTo(eSeverity.Informational, "PowerState", m_PowerState);
				Activities.LogActivity(PowerDeviceControlActivities.GetPowerActivity(m_PowerState));

				//todo: Fix this section?
				if (m_PowerState == ePowerState.PowerOn)
					QueryState();

				RaisePowerStateChanged(value);
			}
		}

		/// <summary>
		/// Gets the current active input address.
		/// </summary>
		public int? ActiveInput
		{
			get { return m_ActiveInput; }
			protected set
			{
				if (value == m_ActiveInput)
					return;

				int? oldInput = m_ActiveInput;
				m_ActiveInput = value;

				Logger.LogSetTo(eSeverity.Informational, "Active Input", m_ActiveInput);

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
		protected AbstractDisplay()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

			m_ConnectionStateManager = new ConnectionStateManager(this)
			{
				ConfigurePort = ConfigurePort
			};
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectedStateChanged;
		}

		public virtual void ConfigurePort(IPort port)
		{
			// Com
			if (port is IComPort)
				(port as IComPort).ApplyDeviceConfiguration(ComSpecProperties);

			// Network (TCP, UDP, SSH)
			if (port is ISecureNetworkPort)
				(port as ISecureNetworkPort).ApplyDeviceConfiguration(NetworkProperties);
			else if (port is INetworkPort)
				(port as INetworkPort).ApplyDeviceConfiguration(NetworkProperties);
		}

		#region Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port);
		}

		/// <summary>
		/// Queues the command to be sent to the device.
		/// </summary>
		/// <param name="command"></param>
		[PublicAPI]
		public void SendCommand([NotNull] ISerialData command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			if (SerialQueue == null)
			{
				Logger.Log(eSeverity.Error, "Unable to send command - SerialQueue is null");
				return;
			}

			SerialQueue.Enqueue(command);
		}

		/// <summary>
		/// Queues the command at the given priority level. Lower values are sent first.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="priority"></param>
		[PublicAPI]
		public void SendCommandPriority([NotNull] ISerialData command, int priority)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			if (SerialQueue == null)
			{
				Logger.Log(eSeverity.Error, "Unable to send command - SerialQueue is null");
				return;
			}

			SerialQueue.EnqueuePriority(command, priority);
		}

		/// <summary>
		/// Queues the command to be sent to the device.
		/// Replaces an existing command if it matches the comparer.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="comparer"></param>
		[PublicAPI]
		public void SendCommand([NotNull] ISerialData command, [NotNull] Func<ISerialData, ISerialData, bool> comparer)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			if (comparer == null)
				throw new ArgumentNullException("comparer");

			if (SerialQueue == null)
			{
				Logger.Log(eSeverity.Error, "Unable to send command - SerialQueue is null");
				return;
			}

			SerialQueue.Enqueue(command, comparer);
		}

		/// <summary>
		/// Queues the command to be sent to the device at the given priority.
		/// Replaces an existing command if it matches the comparer.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="comparer"></param>
		/// <param name="priority"></param>
		[PublicAPI]
		public void SendCommand([NotNull] ISerialData command, [NotNull] Func<ISerialData, ISerialData, bool> comparer, int priority)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			if (comparer == null)
				throw new ArgumentNullException("comparer");

			if (SerialQueue == null)
			{
				Logger.Log(eSeverity.Error, "Unable to send command - SerialQueue is null");
				return;
			}

			SerialQueue.EnqueuePriority(command, comparer, priority, false);
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
		public abstract void SetActiveInput(int address);

		/// <summary>
		/// Clears resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnPowerStateChanged = null;
			OnActiveInputChanged = null;

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

			if (SerialQueue != null)
				SerialQueue.Trust = Trust;

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
			return m_ConnectionStateManager != null
				&& m_ConnectionStateManager.IsOnline;
		}

		protected virtual void PortOnIsOnlineStateChanged(object sender, BoolEventArgs eventArgs)
		{
			UpdateCachedOnlineStatus();
		}

		protected virtual void PortOnConnectedStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			if (m_ConnectionStateManager.IsConnected)
				QueryState();
		}

		protected virtual void RaisePowerStateChanged(ePowerState state)
		{
			RaisePowerStateChanged(state, 0);
		}

		protected void RaisePowerStateChanged(ePowerState state, long expectedDuration)
		{
			OnPowerStateChanged.Raise(this, new DisplayPowerStateApiEventArgs(state, expectedDuration));
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

			serialQueue.OnSerialTransmission += SerialQueueOnSerialTransmission;
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

			serialQueue.OnSerialTransmission -= SerialQueueOnSerialTransmission;
			serialQueue.OnSerialResponse -= SerialQueueOnSerialResponse;
			serialQueue.OnTimeout -= SerialQueueOnTimeout;

			if (serialQueue.Port == null)
				return;

			serialQueue.Port.OnIsOnlineStateChanged -= SerialQueueOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when a command is sent to the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected abstract void SerialQueueOnSerialTransmission(object sender, SerialTransmissionEventArgs args);

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

			settings.Port = m_ConnectionStateManager.PortNumber;

			settings.Trust = Trust;

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

			Trust = false;

			m_ComSpecProperties.ClearComSpecProperties();
			m_NetworkProperties.ClearNetworkProperties();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			// Display inputs rely on available connections
			factory.LoadOriginators<Connection>();

			base.ApplySettingsFinal(settings, factory);

			m_NetworkProperties.Copy(settings);
			m_ComSpecProperties.Copy(settings);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById((int)settings.Port) as ISerialPort;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
				}
			}

			SetPort(port);
			UpdateCachedOnlineStatus();

			Trust = settings.Trust;
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(T settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new DisplayRouteDestinationControl(this, 0));
			addControl(new DisplayPowerDeviceControl(this, 1));
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

			if (m_ConnectionStateManager != null)
				yield return m_ConnectionStateManager.Port;
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
