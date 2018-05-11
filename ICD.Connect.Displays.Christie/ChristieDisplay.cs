using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Displays.Christie
{
	public sealed class ChristieDisplay : AbstractDisplay<ChristieDisplaySettings>
	{
		#region Constants

		private const int MAX_RETRY_ATTEMPTS = 50;
		private const string COMMAND_HEADER = "\xBE\xEF\x03\x06\x00";

		public const char RESPONSE_SUCCESS = '\x06';
		public const char RESPONSE_BAD_COMMAND = '\x15';
		public const char RESPONSE_ERROR = '\x1C';
		public const char RESPONSE_DATA_REPLY = '\x1D';

		private const string POWER_ON = COMMAND_HEADER + "\xBA\xD2\x01\x00\x00\x60\x01\x00";
		private const string POWER_OFF = COMMAND_HEADER + "\x2A\xD3\x01\x00\x00\x60\x00\x00";
		private const string POWER_QUERY = COMMAND_HEADER + "\x19\xD3\x02\x00\x00\x60\x00\x00";

		private const string INPUT_HDMI_1 = COMMAND_HEADER + "\x0E\xD2\x01\x00\x00\x20\x03\x00";
		private const string INPUT_HDMI_2 = COMMAND_HEADER + "\x6E\xD6\x01\x00\x00\x20\x0D\x00";
		private const string INPUT_QUERY = COMMAND_HEADER + "\xCD\xD2\x02\x00\x00\x20\x00\x00";

		private const string ASPECT_NORMAL = COMMAND_HEADER + "\x5E\xDD\x01\x00\x08\x20\x10\x00";
		private const string ASPECT_16_X9 = COMMAND_HEADER + "\x0E\xD1\x01\x00\x08\x20\x01\x00";
		private const string ASPECT_4_X3 = COMMAND_HEADER + "\x9E\xD0\x01\x00\x08\x20\x00\x00";
		private const string ASPECT_QUERY = COMMAND_HEADER + "\xAD\xD0\x02\x00\x08\x20\x00\x00";

		private static readonly Dictionary<eScalingMode, string> s_ScalingModeMap =
			new Dictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, ASPECT_16_X9},
				{eScalingMode.Square4X3, ASPECT_4_X3},
				{eScalingMode.NoScale, ASPECT_NORMAL}
			};

		private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2}
		};

		private static readonly Dictionary<bool, string> s_PowerMap = new Dictionary<bool, string>
		{
			{true, POWER_ON},
			{false, POWER_OFF}
		};

		#endregion

		private readonly Dictionary<string, int> m_RetryCounts = new Dictionary<string, int>();
		private readonly SafeCriticalSection m_RetryLock = new SafeCriticalSection();

		private bool? m_RequestedPowerStatus;
		private int? m_RequestedInput;
		private eScalingMode? m_RequestedAspect;

		#region Properties

		public override int InputCount { get { return s_InputMap.Count; } }

		#endregion

		#region Methods

		/// <summary>
		///     Sets and configures the port for communication with the physical display.
		/// </summary>
		public void SetPort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);

			ISerialBuffer buffer = new ChristieDisplayBuffer();
			RateLimitedQueue queue = new RateLimitedQueue(100);
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);
		}

		/// <summary>
		///     Configures a com port for communication with the physical display.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public static void ConfigureComPort(IComPort port)
		{
			port.SetComPortSpec(eComBaudRates.ComspecBaudRate19200,
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
			SendCommand(POWER_ON);
		}

		public override void PowerOff()
		{
			SendCommand(POWER_OFF);
		}

		public override void SetHdmiInput(int address)
		{
			SendCommand(s_InputMap[address]);
		}

		public override void SetScalingMode(eScalingMode mode)
		{
			SendCommand(s_ScalingModeMap[mode]);
		}

		private void SendCommand(string data)
		{
			SendCommand(new SerialData(data));
		}

		#endregion

		#region Settings

		/// <summary>
		///     Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(ChristieDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			if (SerialQueue != null && SerialQueue.Port != null)
				settings.Port = SerialQueue.Port.Id;
			else
				settings.Port = null;
		}

		/// <summary>
		///     Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPort(null);
		}

		/// <summary>
		///     Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(ChristieDisplaySettings settings, IDeviceFactory factory)
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

		#region Private Methods

		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			string data = args.Response;

			switch (data.First())
			{
				case RESPONSE_SUCCESS:
					ParseSuccess(args);
					break;
				case RESPONSE_BAD_COMMAND:
					ParseBadCommand(args);
					break;
				case RESPONSE_DATA_REPLY:
					ParseDataReply(args);
					break;
				case RESPONSE_ERROR:
					ParseError(args);
					break;
				default:
					ParseError(args);
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
		///     Called when a command is successful.
		/// </summary>
		/// <param name="args"></param>
		private void ParseSuccess(SerialResponseEventArgs args)
		{
			string command = args.Data.Serialize();

			// HDMI
			if (s_InputMap.Values.Contains(command))
				SerialQueue.EnqueuePriority(new SerialData(INPUT_QUERY));

			// Scaling Mode
			else if (s_ScalingModeMap.Values.Contains(command))
				SerialQueue.EnqueuePriority(new SerialData(ASPECT_QUERY));

			else
			{
				switch (command)
				{
					case POWER_ON:
					case POWER_OFF:
						m_RequestedPowerStatus = command == POWER_ON;
						SerialQueue.EnqueuePriority(new SerialData(POWER_QUERY));
						break;
				}
			}
		}

		private void ParseError(SerialResponseEventArgs args)
		{
			RetryCommand(args.Data.Serialize());
		}

		private void ParseBadCommand(SerialResponseEventArgs args)
		{
			Log(eSeverity.Error, "Invalid command sent: {0}", StringUtils.ToHexLiteral(args.Data.Serialize()));
		}

		private void ParseDataReply(SerialResponseEventArgs args)
		{
			switch (args.Data.Serialize())
			{
				case POWER_QUERY:
					PowerQueryResponse(args.Response);
					break;
				case ASPECT_QUERY:
					AspectQueryResponse(args.Response);
					break;
				case INPUT_QUERY:
					InputQueryResponse(args.Response);
					break;
			}
		}

		private void InputQueryResponse(string response)
		{
			int responseInput = s_InputMap.Where(p => p.Value[11] == response[1]).Select(p => p.Key).FirstOrDefault();
			if (m_RequestedInput == null)
			{
				HdmiInput = responseInput;
				ResetRetryCount(INPUT_QUERY);
			}
			else
			{
				string command = s_InputMap[m_RequestedInput.Value];
				if (m_RequestedInput == responseInput || GetRetryCount(command) > MAX_RETRY_ATTEMPTS)
				{
					HdmiInput = responseInput;
					ResetRetryCount(command);
				}
				else
				{
					IncrementRetryCount(command);
					SerialQueue.EnqueuePriority(new SerialData(command));
				}
			}
		}

		private void AspectQueryResponse(string response)
		{
			eScalingMode responseScalingMode =
				s_ScalingModeMap.Where(p => p.Value[11] == response[1]).Select(p => p.Key).FirstOrDefault();
			if (m_RequestedAspect == null)
			{
				ScalingMode = responseScalingMode;
				ResetRetryCount(ASPECT_QUERY);
			}
			else
			{
				string command = s_ScalingModeMap[m_RequestedAspect.Value];
				if (m_RequestedAspect == responseScalingMode || GetRetryCount(command) > MAX_RETRY_ATTEMPTS)
				{
					ScalingMode = responseScalingMode;
					ResetRetryCount(command);
				}
				else
				{
					IncrementRetryCount(command);
					SerialQueue.EnqueuePriority(new SerialData(command));
				}
			}
		}

		private void PowerQueryResponse(string response)
		{
			bool responsePower = s_PowerMap.Where(p => p.Value[11] == response[1]).Select(p => p.Key).FirstOrDefault();
			if (m_RequestedPowerStatus == null)
			{
				IsPowered = responsePower;
				ResetRetryCount(POWER_QUERY);
			}
			else
			{
				string command = s_PowerMap[m_RequestedPowerStatus.Value];
				if (m_RequestedPowerStatus == responsePower || GetRetryCount(command) > MAX_RETRY_ATTEMPTS)
				{
					IsPowered = responsePower;
					ResetRetryCount(command);
				}
				else
				{
					IncrementRetryCount(command);
					SerialQueue.EnqueuePriority(new SerialData(command));
				}
			}
		}

		private void RetryCommand(string command)
		{
			//Log(eSeverity.Error, "Command {0} failed.", StringUtils.ToMixedReadableHexLiteral(args.Data));
			IncrementRetryCount(command);
			if (GetRetryCount(command) <= MAX_RETRY_ATTEMPTS)
			{
				SerialQueue.EnqueuePriority(new SerialData(command));
			}
			else
			{
				Log(eSeverity.Error, "Command {0} hit the retry limit.", StringUtils.ToMixedReadableHexLiteral(command));
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

		protected override void QueryState()
		{
			SendCommand(POWER_QUERY);

			if (!IsPowered)
				return;

			SendCommand(INPUT_QUERY);
			SendCommand(ASPECT_QUERY);
		}

		#endregion
	}
}
