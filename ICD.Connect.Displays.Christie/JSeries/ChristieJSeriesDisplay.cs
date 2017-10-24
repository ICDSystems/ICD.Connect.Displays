using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings.Core;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Christie.JSeries
{
	public sealed class ChristieJSeriesDisplay : AbstractDisplay<ChristieJSeriesDisplaySettings>
	{
		#region Constants

		private const int MAX_RETRY_ATTEMPTS = 50;

		private const string POWER_ON = "(PWR1)";
		private const string POWER_OFF = "(PWR0)";
		private const string POWER_QUERY = "(PWR?)";

		private const string INPUT_CHANNEL = "(SIN{0})";
		private const string INPUT_QUERY = "(SIN?)";

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

		public override int InputCount { get { return 1; } }

		#endregion

		#region Methods

		/// <summary>
		///     Sets and configures the port for communication with the physical display.
		/// </summary>
		public void SetPort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);

			ISerialBuffer buffer = new ChristieJSeriesDisplayBuffer();
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
			port.SetComPortSpec(eComBaudRates.ComspecBaudRate115200,
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
			if (address != 1)
				throw new ArgumentOutOfRangeException("address");

			// HDMI 1 is input 4
			string command = string.Format(INPUT_CHANNEL, 4);
			SendCommand(command);
		}

		public override void SetScalingMode(eScalingMode mode)
		{
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
		protected override void CopySettingsFinal(ChristieJSeriesDisplaySettings settings)
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
		protected override void ApplySettingsFinal(ChristieJSeriesDisplaySettings settings, IDeviceFactory factory)
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

			if (data.Contains("ERR"))
				ParseError(args);

			else if (data.Contains("PWR"))
				PowerQueryResponse(args.Response);

			else if (data.Contains("CHA"))
				InputQueryResponse(args.Response);
		}

		private void ParseError(SerialResponseEventArgs args)
		{
			// (65535 00000 ERR00005 "ITP: Too Few Parameters")
			const string pattern = @"\(65535 00000 ERR(\d+) ""(.*)""\)";

			Regex regex = new Regex(pattern);
			Match match = regex.Match(args.Response);

			if (!match.Success)
				return;

			int code = int.Parse(match.Groups[1].Value);
			string message = match.Groups[2].Value;

			if (args.Data == null)
				Log(eSeverity.Error, "Error {0} - {1}", code, message);
			else
				Log(eSeverity.Error, "Invalid command sent: {0} - Error {1} - {2}", args.Data.Serialize(), code, message);
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

		private void InputQueryResponse(string response)
		{
			if (!response.StartsWith("(CHA!"))
				return;

			response = response.Substring(5).TrimEnd(')');

			int responseInput;
			if (!StringUtils.TryParse(response, out responseInput))
				return;

			if (m_RequestedInput == null)
			{
				HdmiInput = responseInput;
				ResetRetryCount(INPUT_QUERY);
			}
			else
			{
				string command = string.Format(INPUT_CHANNEL, m_RequestedInput.Value);
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

		private void PowerQueryResponse(string response)
		{
			if (!response.StartsWith("(PWR!"))
				return;

			bool isPoweredOff = response == "(PWR!0)";
			bool isPowered = response == "(PWR!1)";

			if (!isPoweredOff && !isPowered)
				return;

			bool responsePower = isPowered;

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
		}

		#endregion
	}
}
