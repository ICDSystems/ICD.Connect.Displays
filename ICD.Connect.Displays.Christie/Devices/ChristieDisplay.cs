using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Christie.Devices
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

		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2}
		};

		private static readonly BiDictionary<ePowerState, string> s_PowerMap = new BiDictionary<ePowerState, string>
		{
			{ePowerState.PowerOn, POWER_ON},
			{ePowerState.PowerOff, POWER_OFF}
		};

		#endregion

		private readonly Dictionary<string, int> m_RetryCounts = new Dictionary<string, int>();
		private readonly SafeCriticalSection m_RetryLock = new SafeCriticalSection();

		private ePowerState? m_RequestedPowerStatus;
		private int? m_RequestedInput;

		#region Methods

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		public override void ConfigurePort(IPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new ChristieDisplayBuffer();
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port as ISerialPort);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);
		}

		public override void PowerOn()
		{
			SendCommand(POWER_ON);
		}

		public override void PowerOff()
		{
			SendCommand(POWER_OFF);
		}

		public override void SetActiveInput(int address)
		{
			SendCommand(s_InputMap.GetValue(address));
		}

		private void SendCommand(string data)
		{
			SendCommand(new SerialData(data));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called when a command is sent to the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialTransmission(object sender, SerialTransmissionEventArgs args)
		{
			if (!Trust)
				return;

			string command = args.Data.Serialize();

			if (s_PowerMap.ContainsValue(command))
			{
				PowerState = s_PowerMap.GetKey(command);
				return;
			}

			if (s_InputMap.ContainsValue(command))
			{
				ActiveInput = s_InputMap.GetKey(command);
				return;
			}
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			string data = args.Response;
			if (string.IsNullOrEmpty(data))
				return;

			switch (data[0])
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
			ISerialData command = args.Data;
			if (command == null)
				return;

			RetryCommand(command.Serialize());
		}

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="args"></param>
		private void ParseSuccess(SerialResponseEventArgs args)
		{
			ISerialData command = args.Data;
			if (command == null)
				return;

			string data = args.Data.Serialize();

			// HDMI
			if (s_InputMap.Values.Contains(data))
				SendCommandPriority(new SerialData(INPUT_QUERY), 0);

			else
			{
				switch (data)
				{
					case POWER_ON:
					case POWER_OFF:
						m_RequestedPowerStatus = data == POWER_ON ? ePowerState.PowerOn : ePowerState.PowerOff;
						SendCommandPriority(new SerialData(POWER_QUERY), 0);
						break;
				}
			}
		}

		private void ParseError(SerialResponseEventArgs args)
		{
			ISerialData command = args.Data;
			if (command == null)
				return;

			RetryCommand(command.Serialize());
		}

		private void ParseBadCommand(SerialResponseEventArgs args)
		{
			Logger.Log(eSeverity.Error, "Invalid command sent: {0}", StringUtils.ToHexLiteral(args.Data.Serialize()));
		}

		private void ParseDataReply(SerialResponseEventArgs args)
		{
			ISerialData command = args.Data;
			if (command == null)
				return;

			switch (command.Serialize())
			{
				case POWER_QUERY:
					PowerQueryResponse(args.Response);
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
				ActiveInput = responseInput;
				ResetRetryCount(INPUT_QUERY);
			}
			else
			{
				string command = s_InputMap.GetValue(m_RequestedInput.Value);
				if (m_RequestedInput == responseInput || GetRetryCount(command) > MAX_RETRY_ATTEMPTS)
				{
					ActiveInput = responseInput;
					ResetRetryCount(command);
				}
				else
				{
					IncrementRetryCount(command);
					SendCommandPriority(new SerialData(command), 0);
				}
			}
		}

		private void PowerQueryResponse(string response)
		{
			ePowerState responsePower = s_PowerMap.Where(p => p.Value[11] == response[1]).Select(p => p.Key).FirstOrDefault();
			if (m_RequestedPowerStatus == null)
			{
				PowerState = responsePower;
				ResetRetryCount(POWER_QUERY);
			}
			else
			{
				string command = s_PowerMap.GetValue(m_RequestedPowerStatus.Value);
				if (m_RequestedPowerStatus == responsePower || GetRetryCount(command) > MAX_RETRY_ATTEMPTS)
				{
					PowerState = responsePower;
					ResetRetryCount(command);
				}
				else
				{
					IncrementRetryCount(command);
					SendCommandPriority(new SerialData(command), 0);
				}
			}
		}

		private void RetryCommand(string command)
		{
			//Log(eSeverity.Error, "Command {0} failed.", StringUtils.ToMixedReadableHexLiteral(args.Data));
			IncrementRetryCount(command);
			if (GetRetryCount(command) <= MAX_RETRY_ATTEMPTS)
			{
				SendCommandPriority(new SerialData(command), 0);
			}
			else
			{
				Logger.Log(eSeverity.Error, "Command {0} hit the retry limit.", StringUtils.ToMixedReadableHexLiteral(command));
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

			if (PowerState != ePowerState.PowerOn)
				return;

			SendCommand(INPUT_QUERY);
		}

		#endregion
	}
}
