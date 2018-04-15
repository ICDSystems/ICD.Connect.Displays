using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Sharp
{
	/// <summary>
	/// SharpDisplay provides methods for interacting with a Sharp TV.
	/// </summary>
	public sealed class SharpDisplay : AbstractDisplayWithAudio<SharpDisplaySettings>
	{
		private const int MAX_RETRY_ATTEMPTS = 50;
		private const long TIMER_MS = 3 * 1000;

		/// <summary>
		/// Maps the Sharp view mode to the command.
		/// </summary>
		private static readonly Dictionary<int, string> s_ViewModeMap =
			new Dictionary<int, string>
			{
				{2, SharpDisplayCommands.SCALING_MODE_4_X3},
				{3, SharpDisplayCommands.SCALING_MODE_ZOOM},
				{4, SharpDisplayCommands.SCALING_MODE_16_X9},
				{8, SharpDisplayCommands.SCALING_MODE_NO_SCALE}
			};

		/// <summary>
		/// Maps scaling mode to command.
		/// </summary>
		private static readonly Dictionary<eScalingMode, string> s_ScalingModeMap =
			new Dictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, SharpDisplayCommands.SCALING_MODE_16_X9},
				{eScalingMode.Square4X3, SharpDisplayCommands.SCALING_MODE_4_X3},
				{eScalingMode.NoScale, SharpDisplayCommands.SCALING_MODE_NO_SCALE},
				{eScalingMode.Zoom, SharpDisplayCommands.SCALING_MODE_ZOOM}
			};

		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
			{1, SharpDisplayCommands.INPUT_HDMI_1},
			{2, SharpDisplayCommands.INPUT_HDMI_2},
			{3, SharpDisplayCommands.INPUT_HDMI_3},
			{4, SharpDisplayCommands.INPUT_HDMI_4}
		};

		private bool m_WarmingUp;

		private readonly SafeTimer m_WarmupRepeatPowerQueryTimer;

		private readonly Dictionary<string, int> m_RetryCounts = new Dictionary<string, int>();
		private readonly SafeCriticalSection m_RetryLock = new SafeCriticalSection();

		#region Properties

		private bool WarmingUp
		{
			get
			{
				return m_WarmingUp;
			}
			set
			{
				if (value == m_WarmingUp)
					return;

				m_WarmingUp = value;

				Log(eSeverity.Informational, "WarmingUp set to {0}", m_WarmingUp);

				if (m_WarmingUp)
				{
					m_WarmupRepeatPowerQueryTimer.Reset(TIMER_MS);
				}
				else
				{
					m_WarmupRepeatPowerQueryTimer.Stop();
					QueryState();
				}
			}
		}

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		public override int InputCount { get { return s_InputMap.Count; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SharpDisplay()
		{
			m_WarmupRepeatPowerQueryTimer = SafeTimer.Stopped(WarmupRepeatPowerQueryTimerOnElapsed);
		}

		/// <summary>
		/// Clears resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			m_WarmupRepeatPowerQueryTimer.Dispose();

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		public void SetPort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);

			if (port != null)
			{
				port.DebugRx = eDebugMode.Ascii;
				port.DebugTx = eDebugMode.Ascii;
			}

			ISerialBuffer buffer = new DelimiterSerialBuffer(SharpDisplayCommands.RETURN[0]);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);
		}

		/// <summary>
		/// Configures a com port for communication with the physical display.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public static void ConfigureComPort(IComPort port)
		{
			port.SetComPortSpec(eComBaudRates.ComspecBaudRate9600,
			                    eComDataBits.ComspecDataBits8,
			                    eComParityType.ComspecParityNone,
			                    eComStopBits.ComspecStopBits1,
			                    eComProtocolType.ComspecProtocolRS232,
			                    eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
			                    eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
			                    false);
		}

		public override void PowerOn()
		{
			SendCommand(SharpDisplayCommands.POWER_ON);
			SendCommand(SharpDisplayCommands.POWER_QUERY);
		}

		public override void PowerOff()
		{
			// So we can PowerOn the TV later.
			PowerOnCommand();

			SendCommand(SharpDisplayCommands.POWER_OFF);
			SendCommand(SharpDisplayCommands.POWER_QUERY);
		}

		public override void MuteOn()
		{
			SendCommand(SharpDisplayCommands.MUTE_ON);
			SendCommand(SharpDisplayCommands.MUTE_QUERY);
		}

		public override void MuteOff()
		{
			SendCommand(SharpDisplayCommands.MUTE_OFF);
			SendCommand(SharpDisplayCommands.MUTE_QUERY);
		}

		public override void MuteToggle()
		{
			SendCommand(SharpDisplayCommands.MUTE_TOGGLE);
			SendCommand(SharpDisplayCommands.MUTE_QUERY);
		}

		[PublicAPI]
		public void PowerOnCommand()
		{
			SendCommand(SharpDisplayCommands.POWER_ON_COMMAND);
		}

		protected override void VolumeSetRawFinal(float raw)
		{
            if (!IsPowered)
                return;

			string command = SharpDisplayCommands.GetCommand(SharpDisplayCommands.VOLUME, raw.ToString());

			SendCommand(command, CommandComparer);
			SendCommand(SharpDisplayCommands.VOLUME_QUERY, CommandComparer);
			SendCommand(SharpDisplayCommands.MUTE_QUERY, CommandComparer);
		}

		public override void VolumeUpIncrement()
		{
            if (!IsPowered)
                return;

			SendCommand(SharpDisplayCommands.VOLUME_UP, CommandComparer);
			SendCommand(SharpDisplayCommands.VOLUME_QUERY, CommandComparer);
			SendCommand(SharpDisplayCommands.MUTE_QUERY, CommandComparer);
		}

		public override void VolumeDownIncrement()
		{
            if (!IsPowered)
                return;

			SendCommand(SharpDisplayCommands.VOLUME_DOWN, CommandComparer);
			SendCommand(SharpDisplayCommands.VOLUME_QUERY, CommandComparer);
			SendCommand(SharpDisplayCommands.MUTE_QUERY, CommandComparer);
		}

		public override void SetHdmiInput(int address)
		{
			SendCommand(s_InputMap[address]);
			SendCommand(SharpDisplayCommands.INPUT_HDMI_QUERY);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode" />
		public override void SetScalingMode(eScalingMode mode)
		{
			SendCommand(s_ScalingModeMap[mode]);
			SendCommand(SharpDisplayCommands.SCALING_MODE_QUERY);
		}

		public void SendCommand(string data)
		{
			SendCommand(new SerialData(data));
		}

		public void SendCommand(string data, Func<string, string, bool> comparer)
		{
			SendCommand(new SerialData(data), (a, b) => comparer(a.Serialize(), b.Serialize()));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Prevents multiple volume commands being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool CommandComparer(string commandA, string commandB)
		{
			// If one is a query and the other is not, the commands are different.
			if (commandA.Contains(SharpDisplayCommands.QUERY) != commandB.Contains(SharpDisplayCommands.QUERY))
				return false;

			// Compare the first 4 characters (e.g. VOLM) to see if it's the same command type.
			return commandA.Substring(0, 4) == commandB.Substring(0, 4);
		}

		private int GetRetryCount(string command)
		{
			m_RetryLock.Enter();

			try
			{
				return m_RetryCounts.ContainsKey(command) ? m_RetryCounts[command] : 0;
			}
			finally
			{
				m_RetryLock.Leave();
			}
		}

		private void WarmupRepeatPowerQueryTimerOnElapsed()
		{
			SendCommand(SharpDisplayCommands.POWER_QUERY);
			SendCommand(SharpDisplayCommands.INPUT_HDMI_QUERY);

			if (m_WarmingUp)
				m_WarmupRepeatPowerQueryTimer.Reset(TIMER_MS);
		}

		/// <summary>
		/// Called when the display has powered on.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Update ourselves.
			SendCommand(SharpDisplayCommands.POWER_QUERY);

			if (!IsPowered)
				return;

			SendCommand(SharpDisplayCommands.INPUT_HDMI_QUERY);
			SendCommand(SharpDisplayCommands.MUTE_QUERY);
			SendCommand(SharpDisplayCommands.SCALING_MODE_QUERY);
			SendCommand(SharpDisplayCommands.VOLUME_QUERY);
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			if (args.Data == null)
				return;

			string responseWithDelimiter = args.Response + SharpDisplayCommands.RETURN;

			switch (responseWithDelimiter)
			{
				case SharpDisplayCommands.ERROR:
					ParseError(args);
					break;

				case SharpDisplayCommands.OK:
					string command = args.Data.Serialize();

					// If we sent a power command we need to update the warmup state
					if (command == SharpDisplayCommands.POWER_ON)
						WarmingUp = true;
					else if (command == SharpDisplayCommands.POWER_OFF)
						WarmingUp = false;

					break;

				default:
					if (args.Data.Serialize().Substring(4, 4) == SharpDisplayCommands.QUERY)
						ParseQuery(args);
					ResetRetryCount(args.Data.Serialize());
					break;
			}
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			RetryCommand(args.Data.Serialize());
		}

		/// <summary>
		/// Called when a query command is successful.
		/// </summary>
		/// <param name="args"></param>
		private void ParseQuery(SerialResponseEventArgs args)
		{
			string response = args.Response.Replace(SharpDisplayCommands.RETURN, "");

			int responseValue;
			if (!StringUtils.TryParse(response, out responseValue))
				return;

			switch (args.Data.Serialize())
			{
				case SharpDisplayCommands.POWER_QUERY:
					bool powered = responseValue == 1;

					// Set the powered state if we are not warming up, OR the new power state is false
					if (!WarmingUp || !powered)
					{
						IsPowered = powered;
						WarmingUp = false;
					}
					break;

				case SharpDisplayCommands.VOLUME_QUERY:
					Volume = (ushort)responseValue;
					WarmingUp = false;
					break;

				case SharpDisplayCommands.MUTE_QUERY:
					IsMuted = responseValue == 1;
					WarmingUp = false;
					break;

				case SharpDisplayCommands.INPUT_HDMI_QUERY:
					HdmiInput = responseValue;
					WarmingUp = false;
					break;

				case SharpDisplayCommands.SCALING_MODE_QUERY:
					if (s_ViewModeMap.ContainsKey(responseValue))
					{
						string command = s_ViewModeMap[responseValue];
						ScalingMode = s_ScalingModeMap.GetKey(command);
					}
					else
						ScalingMode = eScalingMode.Unknown;
					WarmingUp = false;
					break;
			}
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			RetryCommand(args.Data.Serialize());
		}

		private void RetryCommand(string command)
		{
			//Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToMixedReadableHexLiteral(args.Data));
			IncrementRetryCount(command);
			if (GetRetryCount(command) <= MAX_RETRY_ATTEMPTS)
				SerialQueue.EnqueuePriority(new SerialData(command));
			else
			{
				Log(eSeverity.Error, "Command {0} failed too many times and hit the retry limit.",
				    StringUtils.ToMixedReadableHexLiteral(command));
				ResetRetryCount(command);
			}
		}

		private void IncrementRetryCount(string command)
		{
			m_RetryLock.Enter();

			try
			{
				if (m_RetryCounts.ContainsKey(command))
					m_RetryCounts[command]++;
				else
					m_RetryCounts.Add(command, 1);
			}
			finally
			{
				m_RetryLock.Leave();
			}
		}

		private void ResetRetryCount(string command)
		{
			m_RetryLock.Enter();

			try
			{
				m_RetryCounts.Remove(command);
			}
			finally
			{
				m_RetryLock.Leave();
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SharpDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			if (SerialQueue != null && SerialQueue.Port != null)
				settings.Port = SerialQueue.Port.Id;
			else
				settings.Port = null;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPort(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SharpDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Logger.AddEntry(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
			}

			SetPort(port);
		}

		#endregion
	}
}
