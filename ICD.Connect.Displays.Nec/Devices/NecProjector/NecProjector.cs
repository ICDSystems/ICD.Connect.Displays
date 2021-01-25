using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Nec.Devices.NecProjector
{
	public sealed class NecProjector : AbstractProjector<NecProjectorSettings>
	{
		//Command Prioritys
		private const int PRIORITY_POWER_POLL = 8;
		private const int PRIORITY_POWER = 16;
		private const int PRIORITY_INPUT_POLL = 32;
		private const int PRIORITY_INPUT = 64;
		private const int PRIORITY_ASPECT = 128;

		private const int POWER_TRANSIENT_POLL_INTERVAL = 5 * 1000;
		private const int POWER_EQUILIBRIUM_POLL_INTERVAL = 60 * 1000;

		private static readonly BiDictionary<int, string> s_InputAddressMap = new BiDictionary<int, string>
		{
			{1, "\xA1" },	//HDMI 1
			{2, "\xA2" },	//HDMI 2
			{11, "\xA6" },	//DP 1
			{12, "\xA7" },	//DP 2
			{21, "\x01" },	//Computer 1
			{22, "\x02" },	//Computer 2
			{31, "\xBF" },	// HDBT
			{32, "\x20" },	//LAN
			{33, "\xAB" },	//SLOT
			{34, "\x1F" }	//USB-A
		};

		private static readonly BiDictionary<int, string> s_InputResponseAddressMap = new BiDictionary<int, string>
		{
			{1, "\x01\x21" },	//HDMI 1
			{2, "\x02\x21" },	//HDMI 2
			{11, "\x01\x22" },	//DP 1
			{12, "\x02\x22" },	//DP 2
			{21, "\x01\x01" },  //Computer 1
			{22, "\x02\x01" },	//Computer 2
			{31, "\x01\x27" },	//HDBT
			{32, "\x02\x07" },	//LAN
			{33, "\x01\x23" },  //SLOT
			{34, "\x01\x07" }	//USB-A
		};

		private readonly SafeTimer m_PowerTransientTimer;
		private readonly SafeTimer m_PowerEquilibriumTimer;

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public override ePowerState PowerState { get { return base.PowerState; }
			protected set
			{
				base.PowerState = value;

				switch (value)
				{
					case ePowerState.Cooling:
					case ePowerState.Warming:
						StopPowerEquilibriumTimer();
						RestartPowerTransientTimer();
						break;
					case ePowerState.PowerOff:
					case ePowerState.PowerOn:
						StopPowerTransientTimer();
						RestartPowerEquilibriumTimer();
						break;
				}
			}
		}

		public NecProjector()
		{
			m_PowerTransientTimer = SafeTimer.Stopped(PowerTransientTimerCallback);
			m_PowerEquilibriumTimer = SafeTimer.Stopped(PowerEquilibriumTimerCallback);
		}

		#region Power Timers

		private void RestartPowerTransientTimer()
		{
			m_PowerTransientTimer.Reset(POWER_TRANSIENT_POLL_INTERVAL);
		}

		private void StopPowerTransientTimer()
		{
			m_PowerTransientTimer.Stop();
		}

		private void PowerTransientTimerCallback()
		{
			if (PowerState == ePowerState.Warming || PowerState == ePowerState.Cooling || PowerState == ePowerState.Unknown)
				QueryPower();
		}

		private void RestartPowerEquilibriumTimer()
		{
			m_PowerEquilibriumTimer.Reset(POWER_EQUILIBRIUM_POLL_INTERVAL);
		}

		private void StopPowerEquilibriumTimer()
		{
			m_PowerEquilibriumTimer.Stop();
		}

		private void PowerEquilibriumTimerCallback()
		{
			if (PowerState == ePowerState.PowerOn || PowerState == ePowerState.PowerOff)
				QueryState();
		}

		#endregion

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
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			QueryPower();
			QueryInput();
			if (PowerState == ePowerState.PowerOn)
				QueryLamp();
		}

		private void QueryPower()
		{
			SendCommand(eCommandType.RunningStatusRequest);
		}

		private void QueryInput()
		{
			SendCommand(eCommandType.InputStatusRequest);
		}

		private void QueryLamp()
		{
			//Query Lamp 1 (\x00)
			SendCommand(eCommandType.LampInformationRequest, "\x00");
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
				Logger.Log(eSeverity.Error, "TrustMode - unable to cast to NecProjectorCommand - {0:x}", args.Data.Serialize());
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
				if (args.Data != null)
					Logger.Log(eSeverity.Error, "Queue Response - unable to cast to NecProjectorCommand - {0:x}", args.Data.Serialize());
				else
					Logger.Log(eSeverity.Warning, "Unsolicited serial message: {0}", args.Response);
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
				Logger.Log(eSeverity.Error, "Queue Timeout - unable to cast to NecProjectorCommand - {0:x}", args.Data.Serialize());
				return;
			}

			//Retry command
			SendCommandCollapse(command, GetPriorityForCommand(command.CommandType));
		}

		#endregion

		#region Response Handling

		private void ParseSuccess(string argsResponse, NecProjectorCommand command)
		{
			// For successful power on/off commands, set the power state to the tranisent state, so it will poll the state again.
			// Device doesn't immediately report the power state after the power on/off command
			switch (command.CommandType)
			{
				case eCommandType.PowerOn:
					PowerState = ePowerState.Warming;
					break;
				case eCommandType.PowerOff:
					PowerState = ePowerState.Cooling;
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
				case eCommandType.LampInformationRequest:
					ParseLampInformation(argsResponse);
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
				Logger.Log(eSeverity.Error, "Unable to find address for input {0:x}", command.CommandArgs[0]);
		}

		private void ParseInputPoll(string argsResponse)
		{
			string responseAddress = argsResponse.Substring(7, 2);
			int address;
			if (s_InputResponseAddressMap.TryGetKey(responseAddress, out address))
				ActiveInput = address;
			else
				Logger.Log(eSeverity.Warning, "Unable to find input at address {0:x}", responseAddress);
		}

		private void ParsePowerPoll(string argsResponse)
		{

			string data = argsResponse.Substring(5, 16);

			// One of the ways to indicate cooling state
			if (data[3] == '\x01')
			{
				PowerState = ePowerState.Cooling;
				return;
			}

			//Power Command is executing
			if (data[4] == '\x01')
			{
				// This is an undocumented way of determining warming vs cooling excution - may break later
				PowerState = data[5] == '\xFF' ? ePowerState.Cooling : ePowerState.Warming;
				return;
			}

			//Power On/Off states, plus an additional cooling we may never see
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

		private void ParseLampInformation(string argsResponse)
		{
			string data = argsResponse.Substring(5, 6);

			//Check if data is for lamp 1
			if (data[0] != '\x00')
				return;

			uint lampSeconds = 0;

			lampSeconds += (uint)data[5] << 24;
			lampSeconds += (uint)data[4] << 16;
			lampSeconds += (uint)data[3] << 8;
			lampSeconds += (uint)data[2];

			uint lampHours = lampSeconds / 60 / 60;

			LampHours = (int)lampHours;
		}

		private void ParseFailure(string argsResponse, NecProjectorCommand command)
		{
			//Only want to retry on "command execution failed"
			string errorCode = argsResponse.Substring(5, 2);

			string commandArgs = StringUtils.ToHexLiteral(string.Join("", command.CommandArgs));

			switch (errorCode)
			{
				case "\x00\x00":
					Logger.Log(eSeverity.Warning, "Command Error - Not Recognized: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x00\x01":
					Logger.Log(eSeverity.Warning, "Command Error - Not Supported by Model: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x01\x00":
					Logger.Log(eSeverity.Warning, "Command Error - Value Invalid: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x01\x01":
					Logger.Log(eSeverity.Warning, "Command Error - Input Terminal Invalid: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x01\x02":
					Logger.Log(eSeverity.Warning, "Command Error - Language Invalid: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x00":
					Logger.Log(eSeverity.Warning, "Command Error - Memory Allocation Error: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x02":
					Logger.Log(eSeverity.Warning, "Command Error - Memory In Use: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x03":
					Logger.Log(eSeverity.Warning, "Command Error - Value Cannot Be Set: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x04":
					Logger.Log(eSeverity.Warning, "Command Error - Forced Onscreen Mute On: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x06":
					Logger.Log(eSeverity.Warning, "Command Error - Viewer Error: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x07":
					Logger.Log(eSeverity.Warning, "Command Error -No Signal: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x08":
					Logger.Log(eSeverity.Warning, "Command Error - Test Pattern or Filter Displayed: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x09":
					Logger.Log(eSeverity.Warning, "Command Error - No PC Card Inserted: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x0A":
					Logger.Log(eSeverity.Warning, "Command Error - Memory Operation Error: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x0C":
					Logger.Log(eSeverity.Warning, "Command Error - Entry List is Displayed: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x0D":
					Logger.Log(eSeverity.Warning, "Command Error - Command cannot be accepted because power is off: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x02\x0E":
					Logger.Log(eSeverity.Warning, "Command Error - Command execution failed (retrying): {0}:{1}", command.CommandType, commandArgs);
					// Retry
					SendCommandCollapse(command, GetPriorityForCommand(command.CommandType));
					break;
				case "\x02\x0F":
					Logger.Log(eSeverity.Warning, "Command Error - No Authority: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x03\x00":
					Logger.Log(eSeverity.Warning, "Command Error - Specified Gain Number Incorrect: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x03\x01":
					Logger.Log(eSeverity.Warning, "Command Error - Specified Gain Invalid: {0}:{1}", command.CommandType, commandArgs);
					break;
				case "\x03\x02":
					Logger.Log(eSeverity.Warning, "Command Error - Adjustment Failed: {0}:{1}", command.CommandType, commandArgs);
					break;
			}
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("PollPower", "Poll the projector for current power status", () => QueryPower());
			yield return new ConsoleCommand("PollInput", "Poll the projector for current input status" ,() => QueryInput());


		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion

		private void SendCommand(eCommandType commandType, params string[] args)
		{
			SendCommandPriority(new NecProjectorCommand(commandType, args), NecProjectorCommand.CommandComparer, GetPriorityForCommand(commandType));
		}

		private void SendCommandCollapse(NecProjectorCommand command, int priority)
		{
			SendCommandPriority(command, NecProjectorCommand.CommandComparer, priority);
		}

		public override void ConfigurePort(IPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new NecProjectorSerialBuffer(this);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port as ISerialPort);
			queue.SetBuffer(buffer);
			queue.Timeout = 15 * 1000;

			SetSerialQueue(queue);

			ISerialPort serialPort = port as ISerialPort;
			if (serialPort != null && serialPort.IsConnected)
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
