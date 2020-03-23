using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Barco.VideoWallDisplay
{
	public sealed class BarcoVideoWallDisplay : AbstractDisplay<BarcoVideoWallDisplaySettings>
	{
		#region Commands
		internal const string TERMINATOR = "\x0D\x0A";
		internal const string DELIMITER = "\x20";

		private const string POWER_ON = "On";
		private const string POWER_OFF = "idle";

		private const string INPUT_HDMI_1 = "HDMI1";
		private const string INPUT_HDMI_2 = "HDMI2";
		private const string INPUT_DISPLAY_PORT_1 = "DisplayPort1";
		private const string INPUT_DISPLAY_PORT_2 = "DisplayPort2";
		private const string INPUT_DVI_1 = "DVI1";
		private const string INPUT_DVI_2 = "DVI2";
		private const string INPUT_OPS_1 = "OPS1";

		private const string RESPONSE_CONNECTED = "connected";
		private const string RESPONSE_DONE = "done";
		private const string RESPONSE_ERROR = "Error";

		private const string DEVICE_WALL = "wall";
		#endregion

		#region Fields

		private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2},
			{3, INPUT_DISPLAY_PORT_1},
			{4, INPUT_DISPLAY_PORT_2},
			{5, INPUT_DVI_1},
			{6, INPUT_DVI_2},
			{7, INPUT_OPS_1}
		};

		#endregion

		#region Properties

		/// <summary>
		/// Wall device ID, as set on the Barco BCM
		/// </summary>
		private string WallDeviceId { get; set; }

		/// <summary>
		/// Wall device to control the input on
		/// This will typically be a single device,
		/// with other devices cascaded off this one.
		/// Can be set to "wall" in the rare event the entire wall needs to switch inputs
		/// Defaults to "1,1" for the top-left display
		/// </summary>
		private string WallInputControlDevice { get; set; }

		#endregion

		#region Public Methods
		/// <summary>
		/// Powers on the wall
		/// </summary>
		public override void PowerOn()
		{
			SendPowerCommand(true);
		}

		/// <summary>
		/// Powers off the wall
		/// </summary>
		public override void PowerOff()
		{
			SendPowerCommand(false);
		}

		/// <summary>
		/// Sets the input.
		/// Only sets the input on the dispaly specified in WallInputControlDevice
		/// </summary>
		/// <param name="address"></param>
		public override void SetActiveInput(int address)
		{
			if (!s_InputMap.ContainsKey(address))
			{
				Log(eSeverity.Error, "No input at address {0}", address);
				return;
			}

			BarcoVideoWallCommand command = new BarcoVideoWallCommand
			{
				WallDisplayId = WallDeviceId,
				CommandKeyword = eCommandKeyword.Set,
				Command = eCommand.SelInput,
				Device = WallInputControlDevice,
				Attribute = s_InputMap[address]
			};

			SendCommand(command);
		}

		#endregion

		#region Private/Protected Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new MultiDelimiterSerialBuffer(TERMINATOR.ToCharArray());
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			BarcoVideoWallCommand command = args.Data as BarcoVideoWallCommand;
			if (command == null)
			{
				Log(eSeverity.Error, "Unknown Command Timed Out: {1}", args.Data);
				return;
			}

			Log(eSeverity.Debug, "Command Timeout: {0}", command.Serialize());
		}

		/// <summary>
		/// Called when a command is sent to the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialTransmission(object sender, SerialTransmissionEventArgs args)
		{
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			string[] responseParts = args.Response.Split(DELIMITER[0]);

			if (String.IsNullOrEmpty(responseParts[0]))
			{
				Log(eSeverity.Error, "Empty response: {0}", args.Response);
				return;
			}
			if (RESPONSE_CONNECTED.Equals(responseParts[0], StringComparison.OrdinalIgnoreCase))
			{
				InitialConnectPoll();
				return;
			}
			if (RESPONSE_DONE.Equals(responseParts[0], StringComparison.OrdinalIgnoreCase))
			{
				ParseResponseSuccess(args, responseParts);
				return;
			}
			if (RESPONSE_ERROR.Equals(responseParts[0], StringComparison.OrdinalIgnoreCase))
			{
				ParseResponseError(args, responseParts);
				return;
			}

			Log(eSeverity.Warning, "Unmatched response: {0}", args.Response);
		}


		/// <summary>
		/// Don't query the device immediately after commands
		/// The wall reports inconsistent states for a few seconds after commands
		/// Instead, we use the command success response to set states.
		/// </summary>
		protected override void QueryState()
		{
		}

		/// <summary>
		/// Called to poll the device on initial connect
		/// </summary>
		private void InitialConnectPoll()
		{
			BarcoVideoWallCommand powerGetCommand = new BarcoVideoWallCommand
			{
				WallDisplayId = WallDeviceId,
				CommandKeyword = eCommandKeyword.Get,
				Command = eCommand.OpState,
				Device = DEVICE_WALL,
			};

			SendCommand(powerGetCommand);

			BarcoVideoWallCommand inputGetcommand = new BarcoVideoWallCommand
			{
				WallDisplayId = WallDeviceId,
				CommandKeyword =  eCommandKeyword.Get,
				Command = eCommand.SelInput,
				Device = WallInputControlDevice
			};

			SendCommand(inputGetcommand);
		}

		/// <summary>
		/// Build and send power on/off commands
		/// </summary>
		/// <param name="powerState"></param>
		private void SendPowerCommand(bool powerState)
		{
			BarcoVideoWallCommand command = new BarcoVideoWallCommand
			{
				WallDisplayId = WallDeviceId,
				CommandKeyword = eCommandKeyword.Set,
				Command = eCommand.OpState,
				Device = DEVICE_WALL,
				Attribute = powerState ? POWER_ON : POWER_OFF
			};

			SendCommand(command);
		}

		/// <summary>
		/// Parse a successful response from the device
		/// </summary>
		/// <param name="args"></param>
		/// <param name="responseParts"></param>
		private void ParseResponseSuccess(SerialResponseEventArgs args, string[] responseParts)
		{
			if (responseParts.Length < 3)
			{
				Log(eSeverity.Error, "Too short of a response to parse: {0}", args.Response);
				return;
			}

			// Make sure the response is for the right device
			if (!responseParts[1].Equals(WallDeviceId))
				return;

			// Parse command
			if (String.IsNullOrEmpty(responseParts[3]))
			{
				Log(eSeverity.Error, "Unable to get command for response: {0}", args.Response);
				return;
			}

			eCommand command;
			if (!EnumUtils.TryParse(responseParts[3], true, out command))
			{
				Log(eSeverity.Error, "Unable to parse command: {0}", responseParts[3]);
				return;
			}

			switch (command)
			{
				case eCommand.OpState:
					ParsePowerState(args, responseParts);
					break;
				case eCommand.SelInput:
					ParseInput(args, responseParts);
					break;
			}
		}

		/// <summary>
		/// Parse input selection response
		/// </summary>
		/// <param name="args"></param>
		/// <param name="responseParts"></param>
		private void ParseInput(SerialResponseEventArgs args, string[] responseParts)
		{
			if (responseParts.Length <= 5)
			{
				Log(eSeverity.Error, "Input response has too few parts: {0}", args.Response);
				return;
			}

			if (!responseParts[4].Equals(WallInputControlDevice, StringComparison.OrdinalIgnoreCase))
				return;

			KeyValuePair<int, string> input;

			try
			{
				input = s_InputMap.First(kvp => String.Equals(kvp.Value, responseParts[5], StringComparison.OrdinalIgnoreCase));
			}
			catch (InvalidOperationException)
			{
				Log(eSeverity.Error, "Unable to parse input: {0}", responseParts[5]);
				return;
			}

			ActiveInput = input.Key;

		}

		/// <summary>
		/// Parse power state response
		/// </summary>
		/// <param name="args"></param>
		/// <param name="responseParts"></param>
		private void ParsePowerState(SerialResponseEventArgs args, string[] responseParts)
		{
			if (responseParts.Length <= 5)
			{
				Log(eSeverity.Error, "Power response has too few parts: {0}", args.Response);
				return;
			}

			if (!responseParts[4].Equals(DEVICE_WALL, StringComparison.OrdinalIgnoreCase))
				return;

			PowerState = responseParts[5].Equals(POWER_ON, StringComparison.OrdinalIgnoreCase) ? ePowerState.PowerOn : ePowerState.PowerOff;
		}

		/// <summary>
		/// Parse various error conditions, and respond appropriately
		/// </summary>
		/// <param name="args"></param>
		/// <param name="responseParts"></param>
		private void ParseResponseError(SerialResponseEventArgs args, string[] responseParts)
		{
			if (responseParts.Length < 2)
			{
				Log(eSeverity.Error, "Error response has too few parts: {0}", args.Response);
				return;
			}

			if (responseParts[2].Equals("system_busy", StringComparison.OrdinalIgnoreCase))
			{
				Log(eSeverity.Debug, "System Busy, resending command: {0}", args.Response);
				SendCommandPriority(args.Data, 1);
				return;
			}

			if (responseParts[2].Equals("incomplete_action", StringComparison.OrdinalIgnoreCase))
			{
				Log(eSeverity.Debug, "Incomplete Action, resending command: {0}", args.Response);
				SendCommandPriority(args.Data, 1);
				return;
			}

			if (responseParts[2].Equals("syntax_error", StringComparison.OrdinalIgnoreCase))
			{
				Log(eSeverity.Error, "Syntax Error: {0}", args.Response);
				return;
			}

			if (responseParts[2].Equals("not_supported", StringComparison.OrdinalIgnoreCase))
			{
				Log(eSeverity.Error, "Command Not Supported: {0}", args.Response);
				return;
			}

			Log(eSeverity.Error, "Unknown Error: {0}", args.Response);
		
		}

		/// <summary>
		/// Add command the the queue, with appropriate comparator
		/// </summary>
		/// <param name="command"></param>
		private void SendCommand(BarcoVideoWallCommand command)
		{
			SendCommand(command, BarcoVideoWallCommand.Equals);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(BarcoVideoWallDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			WallDeviceId = settings.WallDeviceId;
			WallInputControlDevice = settings.WallInputControlDevice;

			if (String.IsNullOrEmpty(WallDeviceId))
				Log(eSeverity.Error, "Barco UniSee must have WallDeviceId defined");
		}

		/// <summary>
		///     Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			WallDeviceId = null;
			WallInputControlDevice = null;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(BarcoVideoWallDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.WallDeviceId = WallDeviceId;
			settings.WallInputControlDevice = WallInputControlDevice;
		}

		#endregion

		#region Console

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Wall Device ID", WallDeviceId);
			addRow("Wall Input Control Device", WallInputControlDevice);
		}

		#endregion
	}
}