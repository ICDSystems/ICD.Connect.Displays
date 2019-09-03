using System;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Christie.Devices.JSeries
{
	public sealed class ChristieJSeriesDisplay : AbstractDisplay<ChristieJSeriesDisplaySettings>
	{
		private const string POWER = "(PWR{0})";
		private const string INPUT = "(SIN{0})";

		private const char QUERY = '?';

		/// <summary>
		/// Map for Krang addresses to Christie Input Numbers
		/// Christie uses weird addressing for some reason, not sure what's up with that.
		/// NOTE:
		/// Not all projector's HDMI 1 input will correspond with address 1!  Need to verify individually.
		/// Address 1 is linked to "4" for backwards compatabiltiy
		/// </summary>
		private static readonly BiDictionary<int, string> s_InputAddressMap = new BiDictionary<int, string>
		{
			{1, "4"},
			{2, "3"},
			{3, "2"},
			{4, "1"},
			{5, "11"},
			{6, "12"},
			{7, "21"},
			{8, "22"},
			{9, "31"},
			{10, "32"},
			{11, "41"},
			{12, "42"}
		};

		public enum ePowerState
		{
			PowerOff = 0,
			PowerOn = 1,
			Cooldown = 10,
			Warmup = 11,
			AutoShutdown1 = 20,
			AutoShutdown2 = 21,
			AutoShutdown3 = 22,
			EmergencyShutdown = 23
		}

		private const long POWER_HEARTBEAT_INTERVAL = 2 * 1000;

		private readonly SafeTimer m_PowerHeartbeatTimer;
		private ePowerState m_PowerState;

		#region Properties

		public ePowerState PowerState
		{
			get { return m_PowerState; }
			set
			{
				if (value == m_PowerState)
					return;

				m_PowerState = value;

				Log(eSeverity.Informational, "PowerState set to {0}", m_PowerState);

				IsPowered = m_PowerState == ePowerState.PowerOn;

				if (m_PowerState == ePowerState.PowerOn || m_PowerState == ePowerState.PowerOff)
					StopPowerHeartbeat();
				else
					ResetPowerHeartbeat();
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ChristieJSeriesDisplay()
		{
			m_PowerHeartbeatTimer = SafeTimer.Stopped(PowerHeartbeatCallback);
		}

		#region Methods

		/// <summary>
		/// Clears resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			m_PowerHeartbeatTimer.Stop();

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new ChristieJSeriesDisplayBuffer();
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);

			if (port != null && port.IsConnected)
				QueryState();
		}

		public override void PowerOn()
		{
			SendCommand(string.Format(POWER, (int)ePowerState.PowerOn));

			if (!Trust)
				SendCommand(string.Format(POWER, QUERY));
		}

		public override void PowerOff()
		{
			SendCommand(string.Format(POWER, (int)ePowerState.PowerOff));

			if (!Trust)
				SendCommand(string.Format(POWER, QUERY));
		}

		public override void SetActiveInput(int address)
		{
			if (!s_InputAddressMap.ContainsKey(address))
				throw new ArgumentOutOfRangeException("address");

			SendCommand(string.Format(INPUT, s_InputAddressMap.GetValue(address)));

			if (!Trust)
				SendCommand(string.Format(INPUT, QUERY));
		}

		public override void SetScalingMode(eScalingMode mode)
		{
		}

		#endregion

		#region Private Methods

		private void SendCommand(string data)
		{
			SendCommand(new SerialData(data));
		}

		/// <summary>
		/// Called each time the power heartbeat timer elapses.
		/// </summary>
		private void PowerHeartbeatCallback()
		{
			SendCommand(string.Format(POWER, QUERY));
		}

		private void StopPowerHeartbeat()
		{
			m_PowerHeartbeatTimer.Stop();
		}

		private void ResetPowerHeartbeat()
		{
			m_PowerHeartbeatTimer.Reset(POWER_HEARTBEAT_INTERVAL, POWER_HEARTBEAT_INTERVAL);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Log(eSeverity.Error, "Command {0} timed out.", args.Data.Serialize());
		}

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

			if (command.Contains(QUERY))
				return;

			if (command.Contains("PWR"))
			{
				IsPowered = command.Contains("1");
				return;
			}

			if (command.Contains("SIN"))
			{
				int address;

				string inputCommand = command.Substring(4, command.Length - 5);

				if (s_InputAddressMap.TryGetKey(inputCommand, out address))
					ActiveInput = address;

				return;
			}
		}

		/// <summary>
		/// Called when we get a response from the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			string data = args.Response;

			if (data.Contains("ERR"))
				ParseError(args);

			else if (data.Contains("PWR!"))
				PowerQueryResponse(args.Response);

			else if (data.Contains("SIN!"))
				InputQueryResponse(args.Response);

			//Not sure if this is actually correct or not?
			else if (data.Contains("CHA!"))
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

		private void InputQueryResponse(string response)
		{
			string result = GetValue(response);
			if (result == null)
				return;

			int responseInput;

			if (!s_InputAddressMap.TryGetKey(result, out responseInput))
			{
				ActiveInput = null;
				return;
			}

			ActiveInput = responseInput;
		}

		private void PowerQueryResponse(string response)
		{
			string result = GetValue(response);
			if (result == null)
				return;

			int responsePower;
			if (!StringUtils.TryParse(result, out responsePower))
				return;

			PowerState = (ePowerState)responsePower;
		}

		/// <summary>
		/// Gets the value from the given response. Returns null if not a response.
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		private static string GetValue(string response)
		{
			int start = response.IndexOf('!');
			if (start < 0)
				return null;

			response = response.Substring(start + 1);
			
			if (response.EndsWith(")"))
				response = response.Substring(0, response.Length - 1);

			return response;
		}

		/// <summary>
		/// Queries the current state of the device.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			SendCommand(string.Format(POWER, QUERY));

			if (!IsPowered)
				return;

			SendCommand(string.Format(INPUT, QUERY));
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

			addRow("Power State", PowerState);
		}

		#endregion
	}
}
