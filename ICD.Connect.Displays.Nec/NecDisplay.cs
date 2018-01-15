﻿using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Nec
{
	public sealed class NecDisplay : AbstractDisplayWithAudio<NecDisplaySettings>
	{
		private const byte ASPECT_PAGE = 0x02;
		private const byte ASPECT_CODE = 0x70;

		private const byte VOLUME_PAGE = 0x00;
		private const byte VOLUME_CODE = 0x62;

		private const byte MUTE_PAGE = 0x00;
		private const byte MUTE_CODE = 0x8D;

		private const byte INPUT_PAGE = 0x00;
		private const byte INPUT_CODE = 0x60;

		private const ushort ASPECT_16_X9 = 0x03;
		private const ushort ASPECT_4_X3 = 0x01;
		private const ushort ASPECT_OFF = 0x07;
		private const ushort ASPECT_ZOOM = 0x04;

		private const ushort UNMUTE = 0x00;
		private const ushort MUTE = 0x01;

		private const ushort INPUT_HDMI_1 = 0x11;
	    private const ushort INPUT_HDMI_2 = 0x12;
	    private const ushort INPUT_HDMI_3 = 0x13;

		// Commands are kinda weird, need to send a specific array of bytes
		private static readonly byte[] s_PowerQuery = {0x30, 0x31, 0x44, 0x36};
		private static readonly byte[] s_PowerControl = {0x43, 0x32, 0x30, 0x33, 0x44, 0x36};
		private static readonly byte[] s_PowerOn = {0x30, 0x30, 0x30, 0x31};
		private static readonly byte[] s_PowerOff = {0x30, 0x30, 0x30, 0x34};

		private const ushort VOLUME_INCREMENT = 1;

		/// <summary>
		/// Maps scaling mode to command.
		/// </summary>
		private static readonly Dictionary<eScalingMode, ushort> s_ScalingModeMap =
			new Dictionary<eScalingMode, ushort>
			{
				{eScalingMode.Wide16X9, ASPECT_16_X9},
				{eScalingMode.Square4X3, ASPECT_4_X3},
				{eScalingMode.NoScale, ASPECT_OFF},
				{eScalingMode.Zoom, ASPECT_ZOOM}
			};

		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly Dictionary<int, ushort> s_InputMap = new Dictionary<int, ushort>
		{
			{1, INPUT_HDMI_1},
            {2, INPUT_HDMI_2},
            {3, INPUT_HDMI_3}
		};

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		public override int InputCount { get { return s_InputMap.Count; } }

		#region Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		public void SetPort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);

			ISerialBuffer buffer = new BoundedSerialBuffer(NecDisplayCommand.START_HEADER, NecDisplayCommand.END_MESSAGE);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);

			if (port != null)
				QueryState();
		}

		/// <summary>
		/// Configures a com port for communication with the physical display.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public static void ConfigureComPort(IComPort port)
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
			SendCommand(NecDisplayCommand.Command(s_PowerControl.Concat(s_PowerOn)));
		}

		public override void PowerOff()
		{
			SendCommand(NecDisplayCommand.Command(s_PowerControl.Concat(s_PowerOff)));
		}

		public override void SetHdmiInput(int address)
		{
			SendCommand(NecDisplayCommand.SetParameterCommand(INPUT_PAGE, INPUT_CODE, s_InputMap[address]));
		}

		public override void SetScalingMode(eScalingMode mode)
		{
			SendCommand(NecDisplayCommand.SetParameterCommand(ASPECT_PAGE, ASPECT_CODE, s_ScalingModeMap[mode]));
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

		/// <summary>
		/// Enables mute.
		/// </summary>
		public override void MuteOn()
		{
			SendCommand(NecDisplayCommand.SetParameterCommand(MUTE_PAGE, MUTE_CODE, MUTE));
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public override void MuteOff()
		{
			SendCommand(NecDisplayCommand.SetParameterCommand(MUTE_PAGE, MUTE_CODE, UNMUTE));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sends the volume set command to the device after validation has been performed.
		/// </summary>
		/// <param name="raw"></param>
		protected override void VolumeSetRawFinal(float raw)
		{
            if (!IsPowered)
                return;
			SendCommand(NecDisplayCommand.SetParameterCommand(VOLUME_PAGE, VOLUME_CODE, (ushort)raw), VolumeComparer);
		}

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Query the state of the device
			SendCommand(NecDisplayCommand.Command(s_PowerQuery));

			if (!IsPowered)
				return;

			SendCommand(NecDisplayCommand.GetParameterCommand(VOLUME_PAGE, VOLUME_CODE));
			SendCommand(NecDisplayCommand.GetParameterCommand(INPUT_PAGE, INPUT_CODE));
			SendCommand(NecDisplayCommand.GetParameterCommand(ASPECT_PAGE, ASPECT_CODE));
			SendCommand(NecDisplayCommand.GetParameterCommand(MUTE_PAGE, MUTE_CODE));
		}

		/// <summary>
		/// Prevents multiple volume commands from being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool VolumeComparer(NecDisplayCommand commandA, NecDisplayCommand commandB)
		{
			return commandA.MessageType == commandB.MessageType &&
			       commandA.OpCodePage == commandB.OpCodePage &&
			       commandA.OpCode == commandB.OpCode;
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			NecDisplayCommand response = NecDisplayCommand.FromData(args.Response);

			if (response.MessageType == NecDisplayCommand.COMMAND_REPLY)
				ParseCommand(response);
			else if (response.IsErrorMessage)
				ParseError(response);
			else
				ParseSuccess(response);
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
		}

		/// <summary>
		/// For some reason the NEC specification uses "commands" instead of parameters for power,
		/// so the following is attempting to get the current power status from the replies.
		/// </summary>
		/// <param name="response"></param>
		private void ParseCommand(NecDisplayCommand response)
		{
			byte[] message = response.GetMessageWithoutStartEndCodes().ToArray();

			// Response to power query is prefixed with some unused data
			if (message[4] == 0x44 && message[5] == 0x36)
			{
				if (message[3] != 0x30)
				{
					ParseError(response);
					return;
				}

				IsPowered = message[message.Length - 1] == 0x31;
				return;
			}

			if (message[1] != 0x30)
			{
				ParseError(response);
				return;
			}

			// Response to power control
			if (!message.Skip(2).Take(6).SequenceEqual(s_PowerControl))
				return;

			IsPowered = message[message.Length - 1] == 0x31;
		}

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="response"></param>
		private void ParseSuccess(NecDisplayCommand response)
		{
			switch (response.MessageType)
			{
				case NecDisplayCommand.GET_PARAMETER_REPLY:
				case NecDisplayCommand.SET_PARAMETER_REPLY:

					ushort value = response.CurrentValue;
					byte page = response.OpCodePage;
					byte code = response.OpCode;

					if (page == VOLUME_PAGE && code == VOLUME_CODE)
					{
						Volume = value;
						break;
					}

					if (page == MUTE_PAGE && code == MUTE_CODE)
					{
						IsMuted = value == 1;
						break;
					}

					if (page == ASPECT_PAGE && code == ASPECT_CODE)
					{
						ScalingMode = s_ScalingModeMap.ContainsValue(value)
							              ? s_ScalingModeMap.GetKey(value)
							              : eScalingMode.Unknown;
						break;
					}

					if (page == INPUT_PAGE && code == INPUT_CODE)
					{
						HdmiInput = s_InputMap.ContainsValue(value)
							            ? s_InputMap.GetKey(value)
							            : (int?)null;
					}
					break;
			}
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(NecDisplayCommand args)
		{
			Log(eSeverity.Error, "Command {0} failed.", StringUtils.ToHexLiteral(args.Serialize()));
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(NecDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			if (SerialQueue != null && SerialQueue.Port != null)
				settings.Port = SerialQueue.Port.Id;
			else
				settings.Port = null;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPort(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(NecDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ISerialPort port = null;

			if (settings.Port != null)
				port = factory.GetPortById((int)settings.Port) as ISerialPort;

			if (port == null)
				Logger.AddEntry(eSeverity.Error, "No Com Port with id {0}", settings.Port);

			SetPort(port);
		}

		#endregion
	}
}
