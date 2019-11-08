﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Epson.Devices.EpsonProjector
{
	public sealed class EpsonProjector : AbstractDisplay<EpsonProjectorSettings>
	{
		private enum eEpsonPowerState
		{
			StandbyNetworkOff,
			LampOn,
			Warmup,
			Cooldown,
			StandbyNetworkOn,
			AbnormalityStandby,
			AvStandby,
			PreWarming //This s a state we set when we successfully send power on command, before the projector has responded
		}

		private const string INITIAL_IP_HANDSHAKE = "ESC/VP.net\x10\x03\x00\x00\x00\x00";
		private const string INITIAL_IP_RESPONSE = "ESC/VP.net\x10\x03\x00\x00\x20\x00";

		private const string ERROR_RESPONSE = "ERR\x0d";

		private const string POWER_PREFIX = "PWR";
		private const string POWER_COMMAND_ON = POWER_PREFIX + " ON";
		private const string POWER_COMMAND_OFF = POWER_PREFIX + " OFF";
		private const string POWER_POLL = POWER_PREFIX + "?";

		private const string INPUT_PREFIX = "SOURCE";
		private const string INPUT_COMMAND_FORMAT = INPUT_PREFIX + " {0}";
		private const string INPUT_POLL = INPUT_PREFIX + "?";

		private const string EVENT_PREFIX = "IMEVENT";

		private const int PRIORITY_HANDSHAKE = 1;
		private const int PRIORITY_RETRY = 10;
		private const int PRIORITY_POLL = 100;
		private const int PRIORITY_POWER = 1000;
		private const int PRIORITY_INPUT = 10000;
		private const int PRIORITY_OTHER = 100000;

		private const string COMMAND_SUFFIX = "\x0d";

		/// <summary>
		/// Power Response Codes to EpsonPowerState
		/// </summary>
		private static readonly Dictionary<string, eEpsonPowerState> s_EpsonPowerStateValues = new Dictionary<string, eEpsonPowerState>()
		{
			{"00", eEpsonPowerState.StandbyNetworkOff },
			{"01", eEpsonPowerState.LampOn },
			{"02", eEpsonPowerState.Warmup },
			{"03", eEpsonPowerState.Cooldown },
			{"04", eEpsonPowerState.StandbyNetworkOn },
			{"05", eEpsonPowerState.AbnormalityStandby },
			{"09", eEpsonPowerState.AvStandby }
		};

		private static readonly Dictionary<string, eEpsonPowerState> s_EpsonPowerEventValues = new Dictionary<string, eEpsonPowerState>()
		{
			{"01", eEpsonPowerState.StandbyNetworkOn },
			{"02", eEpsonPowerState.Warmup },
			{"03", eEpsonPowerState.LampOn },
			{"04", eEpsonPowerState.Cooldown },
			{"FF", eEpsonPowerState.AbnormalityStandby }
		};

		/// <summary>
		/// Mapping of EpsonPowerState to PowerState
		/// </summary>
		private static readonly Dictionary<eEpsonPowerState, ePowerState> s_PowerStateMap = new Dictionary<eEpsonPowerState, ePowerState>()
		{
			{eEpsonPowerState.StandbyNetworkOff, ePowerState.PowerOff },
			{eEpsonPowerState.LampOn, ePowerState.PowerOn },
			{eEpsonPowerState.Warmup, ePowerState.Warming },
			{eEpsonPowerState.Cooldown, ePowerState.Cooling },
			{eEpsonPowerState.StandbyNetworkOn, ePowerState.PowerOff },
			{eEpsonPowerState.AbnormalityStandby, ePowerState.PowerOff },
			{eEpsonPowerState.AvStandby, ePowerState.PowerOff },
			{eEpsonPowerState.PreWarming, ePowerState.Warming }
		};

		/// <summary>
		/// Input address to protocol codes
		/// Todo: Add additional input addresses as necessary
		/// </summary>
		private static readonly BiDictionary<int, string> s_InputAddressValues = new BiDictionary<int, string>()
		{
			{1,"11" },
			{2,"21" },
			{3,"31" },
			{4,"41" },
			{11,"12" },
			{12,"22" },
			{13,"32" },
			{14,"42" }
		};

		private bool m_IsNetworkPort;

		private eEpsonPowerState m_EpsonPowerState;

		private eEpsonPowerState EpsonPowerState
		{
			get { return m_EpsonPowerState; }
			set
			{
				if (value == m_EpsonPowerState)
					return;

				m_EpsonPowerState = value;

				PowerState = s_PowerStateMap[m_EpsonPowerState];

				if (PowerState == ePowerState.Cooling || PowerState == ePowerState.PowerOff)
					ActiveInput = null;
			}
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			SendCommandPriority(POWER_COMMAND_ON, PRIORITY_POWER);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			SendCommandPriority(POWER_COMMAND_OFF, PRIORITY_POWER);
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public override void SetActiveInput(int address)
		{
			string inputCode;
			if (!s_InputAddressValues.TryGetValue(address, out inputCode))
				throw new ArgumentException(string.Format("{0} Has no input at address {1}", this, address));

			SendCommandPriority(string.Format(INPUT_COMMAND_FORMAT, inputCode),PRIORITY_INPUT);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetScalingMode(eScalingMode mode)
		{
			//Not Impemented
			//Todo: Investigate Implementation
		}

		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			//todo: Remove Debugging
			port.DebugRx = eDebugMode.MixedAsciiHex;
			port.DebugTx = eDebugMode.MixedAsciiHex;

			m_IsNetworkPort = port is INetworkPort;

			ISerialBuffer buffer = new DelimiterSerialBuffer(':',true);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 15 * 1000;

			SetSerialQueue(queue);

			if (port != null && port.IsConnected)
				QueryState();
		}


		protected override void QueryState()
		{
			PollPower();

			if( PowerState == ePowerState.PowerOn)
				PollInput();
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

			//todo: Implememnt "Trust" mode
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{

			//Empty response for power on command sets pre-warming state
			if (string.IsNullOrEmpty(args.Response))
			{
				if (args.Data.Serialize().Trim().Equals(POWER_COMMAND_ON, StringComparison.OrdinalIgnoreCase))
				{
					EpsonPowerState = eEpsonPowerState.PreWarming;
					return;
				}

				return;
			}

			if (args.Response.Contains("="))
			{
				ParsePollResponse(args.Response);
				return;
			}

			if (args.Response == ERROR_RESPONSE)
			{
				Log(eSeverity.Warning, "Error for command: {0}", args.Data);
				return;
			}

			if (args.Response == INITIAL_IP_RESPONSE)
				return;

			Log(eSeverity.Warning, "Unknown Response:{0}",args.Response);
		}

		private void ParsePollResponse(string response)
		{
			int equalsIndex = response.IndexOf('=');

			string command = response.Substring(0, equalsIndex);
			string args = response.Substring(equalsIndex + 1, response.Length - equalsIndex - 1).Trim();

			switch (command)
			{
				case POWER_PREFIX:
					ParsePowerResponse(args);
					break;
				case INPUT_PREFIX:
					ParseInputResponse(args);
					break;
				case EVENT_PREFIX:
					ParseEventResponse(args);
					break;
			}

		}

		private void ParsePowerResponse(string args)
		{
			eEpsonPowerState state;
			if (s_EpsonPowerStateValues.TryGetValue(args, out state))
			{
				EpsonPowerState = state;
			}
			else
				Log(eSeverity.Error, "Unknown Power State: {0}", args);
		}

		private void ParseInputResponse(string args)
		{
			int input;
			if (s_InputAddressValues.TryGetKey(args, out input))
				ActiveInput = input;
			else
			{
				ActiveInput = null;
				Log(eSeverity.Error, "Unknown Input Address: {0}" , args);
			}
		}

		private void ParseEventResponse(string args)
		{
			string[] parts = args.Split('\x20');

			if (parts.Length >= 2)
			{
				eEpsonPowerState state;
				if (s_EpsonPowerEventValues.TryGetValue(parts[1], out state))
					EpsonPowerState = state;
				else
					Log(eSeverity.Error, "Unknown Power State: {0}", args);
			}
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Log(eSeverity.Warning, "Command timed out, retrying:{0}", args.Data);

			SendCommandPriority(args.Data, PRIORITY_RETRY);
		}

		protected override void PortOnConnectedStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			if (boolEventArgs.Data && m_IsNetworkPort)
				SendCommandPriority(INITIAL_IP_HANDSHAKE, PRIORITY_HANDSHAKE);

			base.PortOnConnectedStateChanged(sender, boolEventArgs);
		}

		private void PollPower()
		{
			SendCommandPriority(POWER_POLL, PRIORITY_POLL);
		}

		private void PollInput()
		{
			SendCommandPriority(INPUT_POLL, PRIORITY_POLL);
		}

		private void SendCommand(string command)
		{
			SendCommand(new SerialData(string.Format("{0}{1}", command, COMMAND_SUFFIX)));
		}

		private void SendCommandPriority(string command, int priority)
		{
			SendCommandPriority(new SerialData(string.Format("{0}{1}", command, COMMAND_SUFFIX)), priority);
		}
	}
}