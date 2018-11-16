using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings.Core;

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

		public string WallDeviceId { get; set; }

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		public override int InputCount { get { return s_InputMap.Count; } }

		#endregion

		#region Public Methods
		public override void PowerOn()
		{
			SendPowerCommand(true);
		}

		public override void PowerOff()
		{
			SendPowerCommand(false);
		}

		public override void SetHdmiInput(int address)
		{
			if (!s_InputMap.ContainsKey(address))
			{
				Logger.AddEntry(eSeverity.Error, "No input at address {0}", address);
				return;
			}

			BarcoVideoWallCommand command = new BarcoVideoWallCommand
			{
				WallDisplayId = WallDeviceId,
				CommandKeyword = eCommandKeyword.Set,
				Command = eCommand.ActiveInput,
				Device = DEVICE_WALL,
				Attribute = s_InputMap[address]
			};

			SendCommand(command);
		}

		/// <summary>
		/// Sets scaling mode
		/// Not supported for Barco UniSee
		/// Does nothing
		/// </summary>
		/// <param name="mode"></param>
		public override void SetScalingMode(eScalingMode mode)
		{
		}

		#endregion

		#region Private/Protected Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		protected override void ConfigurePort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);

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
				Logger.AddEntry(eSeverity.Error, "Unknown Command Timed Out: {1}", args.Data);
				return;
			}

			Logger.AddEntry(eSeverity.Debug, "Command Timeout: {0}", command.Serialize());

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
				Logger.AddEntry(eSeverity.Error, "Empty response: {0}", args.Response);
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

			Logger.AddEntry(eSeverity.Warning, "Unmatched response: {0}", args.Response);
		}

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

		private void ParseResponseSuccess(SerialResponseEventArgs args, string[] responseParts)
		{
			// Make sure the response is for the right device
			if (!responseParts[1].Equals(WallDeviceId))
				return;

			// Parse command
			if (String.IsNullOrEmpty(responseParts[3]))
			{
				Logger.AddEntry(eSeverity.Error, "Unable to get command for response: {0}", args.Response);
				return;
			}

			eCommand command;
			if (!EnumUtils.TryParse(responseParts[3], true, out command))
			{
				Logger.AddEntry(eSeverity.Error, "Unable to parse command: {0}", responseParts[3]);
				return;
			}

			switch (command)
			{
				case eCommand.OpState:
					ParsePowerState(responseParts);
					break;
				case eCommand.SelInput:
				case eCommand.ActiveInput:
					ParseInput(responseParts);
					break;
			}
		}

		private void ParseInput(string[] responseParts)
		{
			throw new NotImplementedException();
		}

		private void ParsePowerState(string[] responseParts)
		{
			if (responseParts.Length <= 5)
			{
				Logger.AddEntry(eSeverity.Error, "Power response has too few parts: {0}", responseParts);
				return;
			}

			if (responseParts[4].Equals(DEVICE_WALL, StringComparison.OrdinalIgnoreCase))
				return;

			IsPowered = responseParts[5].Equals(POWER_ON, StringComparison.OrdinalIgnoreCase);
		}

		private void ParseResponseError(SerialResponseEventArgs args, string[] responseParts)
		{
			throw new NotImplementedException();
		}

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

			if (String.IsNullOrEmpty(WallDeviceId))
				Logger.AddEntry(eSeverity.Error, "Barco UniSee must have WallDeviceId defined");
		}

		/// <summary>
		///     Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			WallDeviceId = null;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(BarcoVideoWallDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.WallDeviceId = WallDeviceId;
		}

		#endregion
	}
}