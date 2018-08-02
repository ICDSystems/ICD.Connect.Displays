﻿using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public sealed class SamsungProDisplay : AbstractDisplayWithAudio<SamsungProDisplaySettings>
	{
		private const byte POWER = 0x11;
		private const byte VOLUME = 0x12;
		private const byte MUTE = 0x13;
		private const byte INPUT = 0x14;
		private const byte SCREEN_MODE = 0x15;

		private const byte INPUT_HDMI_1 = 0x21;
		private const byte INPUT_HDMI_1_PC = 0x22;
		private const byte INPUT_HDMI_2 = 0x23;
		private const byte INPUT_HDMI_2_PC = 0x24;
		private const byte INPUT_HDMI_3 = 0x31;
		private const byte INPUT_HDMI_3_PC = 0x31;
		private const byte INPUT_DISPLAYPORT = 0x25;
		private const byte INPUT_DVI = 0x18;
		private const byte INPUT_DVI_VIDEO = 0x1F;

		private const byte ASPECT_16_X9 = 0x01;
		private const byte ASPECT_WIDE = 0x04;
		private const byte ASPECT_4_X3 = 0x0B;
		private const byte ASPECT_WIDE_FIT = 0x0C;

		private const ushort VOLUME_INCREMENT = 1;

		/// <summary>
		/// Maps scaling mode to command.
		/// </summary>
		private static readonly Dictionary<eScalingMode, byte> s_ScalingModeMap =
			new Dictionary<eScalingMode, byte>
			{
				{eScalingMode.Wide16X9, ASPECT_16_X9},
				{eScalingMode.Square4X3, ASPECT_4_X3},
				{eScalingMode.NoScale, ASPECT_WIDE_FIT},
				{eScalingMode.Zoom, ASPECT_WIDE}
			};

		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly Dictionary<int, byte> s_InputMap = new Dictionary<int, byte>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2},
			{3, INPUT_HDMI_3},
			{4, INPUT_DISPLAYPORT},
			{5, INPUT_DVI},
		};

		private static readonly Dictionary<int, byte> s_InputPcMap = new Dictionary<int, byte>
		{
			{1, INPUT_HDMI_1_PC},
			{2, INPUT_HDMI_2_PC},
			{3, INPUT_HDMI_3_PC},
			{5, INPUT_DVI_VIDEO}
		};

		#region Properties

		/// <summary>
		/// Gets/sets the ID of this tv.
		/// </summary>
		[PublicAPI]
		public byte WallId { get; set; }

		/// <summary>
		/// Gets the number of HDMI inputs.
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

			ISerialBuffer buffer = new SamsungProDisplayBuffer();
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 3 * 1000;

			SetSerialQueue(queue);

			if (port != null)
				SendCommand(new SamsungProCommand(POWER, WallId, 0).ToQuery());
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

		public override void PowerOn()
		{
			SendCommand(new SamsungProCommand(POWER, WallId, 1));
		}

		public override void PowerOff()
		{
			SendCommand(new SamsungProCommand(POWER, WallId, 0));
		}

		public override void SetHdmiInput(int address)
		{
			SendCommand(new SamsungProCommand(INPUT, WallId, s_InputMap[address]));
		}

		public override void SetScalingMode(eScalingMode mode)
		{
			SendCommand(new SamsungProCommand(SCREEN_MODE, WallId, s_ScalingModeMap[mode]));
		}

		public override void MuteOn()
		{
			SendCommand(new SamsungProCommand(MUTE, WallId, 1));
		}

		public override void MuteOff()
		{
			SendCommand(new SamsungProCommand(MUTE, WallId, 0));
		}

		protected override void VolumeSetRawFinal(float raw)
		{
			SendCommand(new SamsungProCommand(VOLUME, WallId, (byte)raw), CommandComparer);

			// Display unmutes on volume change
			SendCommand(new SamsungProCommand(MUTE, WallId, 0).ToQuery(), CommandComparer);
		}

		/// <summary>
		/// Prevents multiple volume commands from being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool CommandComparer(AbstractSamsungProCommand commandA, AbstractSamsungProCommand commandB)
		{
			// If one is a query and the other is not, the commands are different.
			if (commandA.GetType() != commandB.GetType())
				return false;

			// Are the command types the same?
			return commandA.Command == commandB.Command;
		}

		public override void VolumeUpIncrement()
		{
			if (!IsPowered)
				return;

			SetVolume((ushort)(Volume + VOLUME_INCREMENT));
		}

		public override void VolumeDownIncrement()
		{
			if (!IsPowered)
				return;

			SetVolume((ushort)(Volume - VOLUME_INCREMENT));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Important - Some SamsungPro models get really upset if you try to send commands
			// while it's warming up. Sending the queries first by priority seems to solve this problem.

			// Query the state of the device
			SerialQueue.EnqueuePriority(new SamsungProCommand(POWER, WallId, 0).ToQuery(), int.MinValue);

			if (!IsPowered)
				return;

			SerialQueue.EnqueuePriority(new SamsungProCommand(VOLUME, WallId, 0).ToQuery(), int.MinValue);
			SerialQueue.EnqueuePriority(new SamsungProCommand(INPUT, WallId, 0).ToQuery(), int.MinValue);
			SerialQueue.EnqueuePriority(new SamsungProCommand(SCREEN_MODE, WallId, 0).ToQuery(), int.MinValue);
			SerialQueue.EnqueuePriority(new SamsungProCommand(MUTE, WallId, 0).ToQuery(), int.MinValue);
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			IcdConsole.PrintLine(eConsoleColor.Magenta, StringUtils.ToHexLiteral(args.Response));

			SamsungProResponse response = new SamsungProResponse(args.Response);

			switch (response.Header)
			{
				case 0xAA:
					if (response.Id != WallId)
						return;

					switch (response.Code)
					{
						// Normal response
						case 0xFF:
							if (!response.IsValid)
								return;

							if (response.Success)
								ParseSuccess(response);
							else
								ParseError(args);
							break;

						// Cooldown
						case 0xE1:
							// Ignore unsolicited cooldown message
							return;
					}
					break;
				
				case 0xFF:
				case 0x1C:
					switch (response.Code)
					{
						// Warmup
						case 0x1C:
						case 0xC4:
							// Keep sending power query until fully powered on
							SerialQueue.EnqueuePriority(new SamsungProCommand(POWER, WallId, 0).ToQuery());
							break;
					}
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
			Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToHexLiteral(args.Data.Serialize()));

			// Keep sending power query until fully powered on
			if (SerialQueue.TimeoutCount < 10)
				SerialQueue.EnqueuePriority(new SamsungProCommand(POWER, WallId, 0).ToQuery());
		}

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="response"></param>
		private void ParseSuccess(SamsungProResponse response)
		{
			switch (response.Command)
			{
				case POWER:
					byte powerValue = response.Values[0];
					if (powerValue == 1)
						IsPowered = true;
					else if (powerValue == 0)
						IsPowered = false;
					return;

				case VOLUME:
					Volume = response.Values[0];
					return;

				case MUTE:
					byte muteValue = response.Values[0];
					if (muteValue == 1)
						IsMuted = true;
					else if (muteValue == 0)
						IsMuted = false;
					return;

				case INPUT:
					byte inputCode = response.Values[0];
					HdmiInput = s_InputMap.ContainsValue(inputCode)
						            ? s_InputMap.GetKey(inputCode)
						            : s_InputPcMap.ContainsValue(inputCode)
							              ? s_InputPcMap.GetKey(inputCode)
							              : (int?)null;
					break;

				case SCREEN_MODE:
					ScalingMode = s_ScalingModeMap.ContainsValue(response.Values[0])
						              ? s_ScalingModeMap.GetKey(response.Values[0])
						              : eScalingMode.Unknown;
					break;
			}
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			Log(eSeverity.Error, "Command {0} failed.", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			WallId = 0;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SamsungProDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.WallId = WallId;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SamsungProDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			WallId = settings.WallId;
		}

		#endregion

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Wall ID", WallId);
		}
	}
}
