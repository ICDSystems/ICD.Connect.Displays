using System;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Nec.Devices.NecProjector
{
	public sealed class NecProjector : AbstractDisplay<NecProjectorSettings>
	{

		//Command Prioritys
		private const int PRIORITY_POWER_POLL = 8;
		private const int PRIORITY_POWER = 16;
		private const int PRIORITY_INPUT_POLL = 32;
		private const int PRIORITY_INPUT = 64;
		private const int PRIORITY_ASPECT = 100;

		private static readonly BiDictionary<int, string> s_InputAddressMap = new BiDictionary<int, string>()
		{
			{1, "\xA1" }, //HDMI 1
			{2, "\xA2" }, //HDMI 2
			{11, "\x1A" }, //HDMI 1 Alternate
			{12, "\x1B" }, //HDMI 2 Alternate && DP Alternate
			{13, "\xBF" }, // HDBT
			{14, "\x1C" }, //SLOT
			{21, "\xA6" }, //DP 1
			{22, "\xA7" }, //DP 2
			{31, "\x01" }, //Computer 1
			{32, "\x02" }, //Computer 2
			{33, "\x03" }, //Computer 3
			{40, "\xAB" }, //SLOT Alternate
			{41, "\x20" }  //LAN & HDBT Alternate
		};

		#region IDisplay

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			SendCommand(eCommandType.PowerOn);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			SendCommand(eCommandType.PowerOff);
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public override void SetActiveInput(int address)
		{
			string inputCode;
			if (!s_InputAddressMap.TryGetValue(address, out inputCode))
				throw new ArgumentException(string.Format("{0} Has no input at address {1}", this, address));

			SendCommand(eCommandType.InputSwitch, inputCode);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetScalingMode(eScalingMode mode)
		{
		}

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();
			QueryPower();
			QueryInput();
		}

		private void QueryPower()
		{
			SendCommand(eCommandType.RunningStatusRequest);
		}

		private void QueryInput()
		{
			SendCommand(eCommandType.InputStatusRequest);
		}

		#endregion


		#region Serial Queue Callbacks
		/// <summary>
		/// Called when a command is sent to the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialTransmission(object sender, SerialTransmissionEventArgs args)
		{
			if (!Trust)
				return;

			NecProjectorCommand command = args.Data as NecProjectorCommand;

			if (command == null)
			{
				Log(eSeverity.Error, "TrustMode - unable to cast to NecProjectorCommand - {0}", args.Data.Serialize());
				return;
			}

			switch (command.CommandType)
			{
				case eCommandType.PowerOn:
					PowerState = ePowerState.PowerOn;
					break;
				case eCommandType.PowerOff:
					PowerState = ePowerState.PowerOff;
					break;
				case eCommandType.InputSwitch:
					ActiveInput = s_InputAddressMap.GetKey(command.CommandArgs[0]);
					break;
			}
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			NecProjectorCommand command = args.Data as NecProjectorCommand;

			if (command == null)
			{
				Log(eSeverity.Error, "Queue Response - unable to cast to NecProjectorCommand - {0}", args.Data.Serialize());
				return;
			}

			if (NecProjectorCommand.IsResponseSuccess(args.Response))
				ParseSuccess(args.Response, command);
			else
				ParseFailure(args.Response, command);
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			NecProjectorCommand command = args.Data as NecProjectorCommand;

			if (command == null)
			{
				Log(eSeverity.Error, "Queue Timeout - unable to cast to NecProjectorCommand - {0}", args.Data.Serialize());
				return;
			}

			//Retry command at the same priority
			SendCommandCollapse(command, GetPriorityForCommand(command.CommandType));
		}

		#endregion

		#region Response Handling

		private void ParseSuccess(string argsResponse, NecProjectorCommand command)
		{
			switch (command.CommandType)
			{
				case eCommandType.PowerOn:
				case eCommandType.PowerOff:
					QueryPower();
					break;
				case eCommandType.InputSwitch:
					ParseInputSwitch(argsResponse, command);
					break;
				case eCommandType.RunningStatusRequest:
					ParsePowerPoll(argsResponse);
					break;
				case eCommandType.InputStatusRequest:
					ParseInputPoll(argsResponse);
					break;
			}
		}

		private void ParseInputSwitch(string argsResponse, NecProjectorCommand command)
		{
			string data1 = argsResponse.Substring(5, 1);

			// Retry failed commands
			if (data1 != "\x00")
			{
				SendCommandCollapse(command, GetPriorityForCommand(command.CommandType));
				return;
			}

			int address;
			if (s_InputAddressMap.TryGetKey(command.CommandArgs[0], out address))
				ActiveInput = address;
			else
				Log(eSeverity.Error, "Unable to find address for input {0}", command.CommandArgs[0]);
		}

		private void ParseInputPoll(string argsResponse)
		{
			byte data = (byte)argsResponse[6];

			//Add 1, because NEC?
			data++;





			int address;
			if (s_InputAddressMap.TryGetKey(data.ToString(), out address))
				ActiveInput = address;
			else
				Log(eSeverity.Warning, "Unable to find input at address {0}", data);
		}

		private void ParsePowerPoll(string argsResponse)
		{

			string data = argsResponse.Substring(5, 16);

			switch (data[5])
			{
				case '\x00':
				case '\x0F':
				case '\x10':
				case '\x06':
					PowerState = ePowerState.PowerOff;
					break;
				case '\x04':
					PowerState = ePowerState.PowerOn;
					break;
				case '\x05':
					PowerState = ePowerState.Cooling;
					break;
				default:
					PowerState = ePowerState.Unknown;
					break;
					
			}
		}

		private void ParseFailure(string argsResponse, NecProjectorCommand command)
		{
			//Only want to retry on "command execution failed"
			string errorCode = argsResponse.Substring(5, 2);

			switch (errorCode)
			{
				case "\x00\x00":
					Log(eSeverity.Warning, "Command Error - Not Recognized: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x00\x01":
					Log(eSeverity.Warning, "Command Error - Not Supported by Model: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x01\x00":
					Log(eSeverity.Warning, "Command Error - Value Invalid: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x01\x01":
					Log(eSeverity.Warning, "Command Error - Input Terminal Invalid: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x01\x02":
					Log(eSeverity.Warning, "Command Error - Language Invalid: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x00":
					Log(eSeverity.Warning, "Command Error - Memory Allocation Error: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x02":
					Log(eSeverity.Warning, "Command Error - Memory In Use: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x03":
					Log(eSeverity.Warning, "Command Error - Value Cannot Be Set: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x04":
					Log(eSeverity.Warning, "Command Error - Forced Onscreen Mute On: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x06":
					Log(eSeverity.Warning, "Command Error - Viewer Error: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x07":
					Log(eSeverity.Warning, "Command Error -No Signal: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x08":
					Log(eSeverity.Warning, "Command Error - Test Pattern or Filter Displayed: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x09":
					Log(eSeverity.Warning, "Command Error - No PC Card Inserted: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x0A":
					Log(eSeverity.Warning, "Command Error - Memory Operation Error: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x0C":
					Log(eSeverity.Warning, "Command Error - Entry List is Displayed: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x0D":
					Log(eSeverity.Warning, "Command Error - Command cannot be accepted because power is off: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x02\x0E":
					Log(eSeverity.Warning, "Command Error - Command execution failed (retrying): {0}:{1}", command.CommandType, command.CommandArgs);
					// Retry
					SendCommandCollapse(command, GetPriorityForCommand(command.CommandType));
					break;
				case "\x02\x0F":
					Log(eSeverity.Warning, "Command Error - No Authority: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x03\x00":
					Log(eSeverity.Warning, "Command Error - Specified Gain Number Incorrect: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x03\x01":
					Log(eSeverity.Warning, "Command Error - Specified Gain Invalid: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
				case "\x03\x02":
					Log(eSeverity.Warning, "Command Error - Adjustment Failed: {0}:{1}", command.CommandType, command.CommandArgs);
					break;
			}
		}

		#endregion

		private void SendCommand(eCommandType commandType, params string[] args)
		{
			SendCommand(new NecProjectorCommand(commandType, args), NecProjectorCommand.CommandComparer, GetPriorityForCommand(commandType));
		}

		private void SendCommandCollapse(NecProjectorCommand command, int priority)
		{
			SendCommand(command, NecProjectorCommand.CommandComparer, priority);
		}

		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new NecProjectorSerialBuffer();
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 15 * 1000;

			SetSerialQueue(queue);

			if (port != null && port.IsConnected)
				QueryState();
		}

		private static int GetPriorityForCommand(eCommandType command)
		{
			switch (command)
			{
				case eCommandType.RunningStatusRequest:
					return PRIORITY_POWER_POLL;
				case eCommandType.PowerOff:
				case eCommandType.PowerOn:
					return PRIORITY_POWER;
				case eCommandType.InputStatusRequest:
					return PRIORITY_INPUT_POLL;
				case eCommandType.InputSwitch:
					return PRIORITY_INPUT;
				case eCommandType.AspectAdjust:
					return PRIORITY_ASPECT;
				default:
					return int.MaxValue;
			}
		}
	}
}
