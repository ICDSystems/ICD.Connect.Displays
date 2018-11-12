using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.PanasonicClassic.Devices
{
	public sealed class PanasonicClassicDisplay : AbstractDisplayWithAudio<PanasonicClassicDisplaySettings>
	{

		#region Commands

		//When the first char of the actual command is A-E, the combined notation interprets it as a part of 
		// the hex notated first character, ie \x02AMT:0 becomes *MT:0
		// so these commands are entirely in hex

		private const char STX = '\x02';
		private const char ETX = '\x03';

		private const string FAILURE = "\x02\x45\x52\x34\x30\x31\x03";

		private const string POWER_ON = "\x02PON\x03";
		private const string POWER_OFF = "\x02POF\x03";
		private const string QUERY_POWER = "\x02QPW\x03";

		private const string MUTE_ON = "\x02\x41\x4d\x54\x3a\x31\x03";
		private const string MUTE_OFF = "\x02\x41\x4d\x54\x3a\x30\x03";
		private const string QUERY_MUTE = "\x02QAM\x03";

		private const string VOLUME_SET_TEMPLATE = "\x02\x41\x56\x4c\x3a{0}\x03";
		private const string QUERY_VOLUME = "\x02QAV\x03";

		private const string INPUT_TOGGLE = "\x02IMS\x03";
		private const string INPUT_HDMI1 = "\x02IMS:HM1\x03";
		private const string INPUT_HDMI2 = "\x02IMS:HM2\x03";
		private const string INPUT_DVI = "\x02IMS:DV1\x03";
		private const string INPUT_PC = "\x02IMS:PC1\x03";
		private const string INPUT_VIDEO = "\x02IMS:VD1\x03";
		private const string INPUT_USB = "\x02IMS:UD1\x03";

		#endregion

		private const int MAX_RETRY_ATTEMPTS = 500;
		private const int CONNECTION_WAIT_TIMEOUT_MS = 3 * 1000;
		private readonly Dictionary<string, int> m_RetryCounts = new Dictionary<string, int>();
		private readonly SafeCriticalSection m_RetryLock = new SafeCriticalSection();

		private bool m_IsIpControlled;
		private bool? m_ExpectedPowerState;
		private int m_TargetInput = 1;

		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
			{1, INPUT_HDMI1},
			{2, INPUT_HDMI2},
			{3, INPUT_DVI},
			{4, INPUT_PC},
			{5, INPUT_VIDEO},
			{6, INPUT_USB}
		};

		#region Properties

		/// <summary>
		/// Gets the number of inputs.
		/// </summary>
		public override int InputCount { get { return s_InputMap.Count; } }

		#endregion

		#region Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		protected override void ConfigurePort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);

			if (port is AsyncTcpClient)
			{
				m_IsIpControlled = true;
				//Todo: Connection State Manager for IP
			}

			ISerialBuffer buffer = new BoundedSerialBuffer(STX, ETX);
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
		public override void ConfigureComPort(IComPort port)
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

		protected override void QueryState()
		{
			base.QueryState();

			if (!IsPowered)
				return;

			SendNonFormattedCommand(QUERY_VOLUME);
			SendNonFormattedCommand(QUERY_MUTE);
		}

		[PublicAPI]
		public override void PowerOn()
		{
			SendNonFormattedCommandPriority(POWER_ON, 0);
			m_ExpectedPowerState = true;
			QueryPower();
		}

		[PublicAPI]
		public override void PowerOff()
		{
			SendNonFormattedCommandPriority(POWER_OFF, 1);
			m_ExpectedPowerState = false;
			QueryPower();
		}

		[PublicAPI]
		public override void MuteOn()
		{
			if (!IsPowered)
				return;
			SendNonFormattedCommand(MUTE_ON);
			SendNonFormattedCommand(QUERY_MUTE);
		}

		[PublicAPI]
		public override void MuteOff()
		{
			if (!IsPowered)
				return;
			SendNonFormattedCommand(MUTE_OFF);
			SendNonFormattedCommand(QUERY_MUTE);
		}

		[PublicAPI]
		public override void VolumeUpIncrement()
		{
			if (!IsPowered)
				return;
			SendNonFormattedCommand(GenerateSetVolumeCommand((int)Volume + 1));
			SendNonFormattedCommand(QUERY_VOLUME);
		}

		[PublicAPI]
		public override void VolumeDownIncrement()
		{
			if (!IsPowered)
				return;
			SendNonFormattedCommand(GenerateSetVolumeCommand((int)Volume - 1));
			SendNonFormattedCommand(QUERY_VOLUME);
		}

		[PublicAPI]
		protected override void VolumeSetRawFinal(float raw)
		{
			if (!IsPowered)
				return;
			string setVolCommand = GenerateSetVolumeCommand((int)raw);
			SendNonFormattedCommand(setVolCommand);
			SendNonFormattedCommand(QUERY_VOLUME);
		}

		[PublicAPI]
		public override void SetHdmiInput(int address)
		{
			if (!IsPowered)
				return;
			SendNonFormattedCommand(s_InputMap[address]);
			m_TargetInput = address;
		}

		[PublicAPI]
		public override void SetScalingMode(eScalingMode mode)
		{
			//This device does not support scaling modes. Do Nothing.
		}

		[PublicAPI]
		public static string ExtractCommand(string data)
		{
			return data.Substring(1, 3);
		}

		[PublicAPI]
		public static string ExtractParameter(string data, int paramLength)
		{
			return data.Substring(5, paramLength);
		}

		#endregion

		#region Private Methods

		private void QueryPower()
		{
			var input = HdmiInput == null || HdmiInput.Value == 0 ? 1 : HdmiInput.Value;
			var command = s_InputMap[input];
			SendNonFormattedCommandPriority(command, 2);
		}

		/// <summary>
		/// Queues the data to be sent to the physical display.
		/// </summary>
		/// <param name="data"></param>
		private void SendNonFormattedCommand(string data)
		{
			SendNonFormattedCommand(data, (a, b) => a == b);
		}

		/// <summary>
		/// Queues the data to be sent to the physical display.
		/// Replaces an earlier command if found via the comparer.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="comparer"></param>
		private void SendNonFormattedCommand(string data, Func<string, string, bool> comparer)
		{
			SendCommand(new SerialData(data), (a, b) => comparer(a.Serialize(), b.Serialize()));
		}

		/// <summary>
		/// Queues the data to be sent to the physical display at the given priority.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="priority"></param>
		private void SendNonFormattedCommandPriority(string data, int priority)
		{
			SendCommandPriority(new SerialData(data), priority);
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			if (args.Response == FAILURE)
				ParseError(args);
			else
				ParseSuccess(args);
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToHexLiteral(args.Data.Serialize()));
			RetryCommand(args.Data.Serialize());
		}

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="args"></param>
		private void ParseSuccess(SerialResponseEventArgs args)
		{
			string response = args.Response;
			string command = ExtractCommand(response);

			switch (command)
			{
				case "QAM":
					string muted = ExtractParameter(response, 1);
					IsMuted = muted == "1";
					break;
				case "QAV":
					string volume = ExtractParameter(response, 1);
					Volume = int.Parse(volume);
					IsMuted = false;
					break;
				case "IMS":
					if (!IsPowered
						&& m_ExpectedPowerState != null
						&& m_ExpectedPowerState.Value)
					{
						IsPowered = true;
						m_ExpectedPowerState = null;
					}
					else if (IsPowered
							 && m_ExpectedPowerState != null
							 && !m_ExpectedPowerState.Value)
					{
						RetryCommand(args.Data.Serialize());
					}
					else
					{
						HdmiInput = m_TargetInput;
					}
					break;
			}
			ResetRetryCount(args.Data.Serialize());

		}

		private static string GenerateSetVolumeCommand(int volumePercent)
		{
			volumePercent = MathUtils.Clamp(volumePercent, 0, 100);
			string volumeString = volumePercent.ToString();
			// protocol expects volume percent to always be three characters
			while (volumeString.Length < 3)
				volumeString = "0" + volumeString;
			return string.Format(VOLUME_SET_TEMPLATE, volumeString);
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			if (IsPowered
				&& ExtractCommand(args.Data.Serialize()) == "IMS"
				&& m_ExpectedPowerState != null
				&& !m_ExpectedPowerState.Value)
			{
				IsPowered = false;
				m_ExpectedPowerState = null;
				ResetRetryCount(args.Data.Serialize());
				return;
			}

			RetryCommand(args.Data.Serialize());
		}

		private void RetryCommand(string command)
		{
			Log(eSeverity.Debug, "Retry {0}, {1} times", command, GetRetryCount(command));
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

		#endregion
	}
}
