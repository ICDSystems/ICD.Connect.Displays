﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Nec.Devices.NecDisplay
{
	public sealed class NecDisplay : AbstractDisplayWithAudio<NecDisplaySettings>
	{
		private const byte VOLUME_PAGE = 0x00;
		private const byte VOLUME_CODE = 0x62;

		private const byte MUTE_PAGE = 0x00;
		private const byte MUTE_CODE = 0x8D;

		private const byte INPUT_PAGE = 0x00;
		private const byte INPUT_CODE = 0x60;

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
		/// Maps index to an input command.
		/// </summary>
		private static readonly Dictionary<int, ushort> s_InputMap = new Dictionary<int, ushort>
		{
			{1, INPUT_HDMI_1},
            {2, INPUT_HDMI_2},
            {3, INPUT_HDMI_3}
		};

		#region Properties

		public byte MonitorId { get; set; }

		/// <summary>
		/// Returns the features that are supported by this display.
		/// </summary>
		public override eVolumeFeatures SupportedVolumeFeatures
		{
			get
			{
				return eVolumeFeatures.Mute |
					   eVolumeFeatures.MuteAssignment |
					   eVolumeFeatures.MuteFeedback |
					   eVolumeFeatures.Volume |
					   eVolumeFeatures.VolumeAssignment |
					   eVolumeFeatures.VolumeFeedback;
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public NecDisplay()
		{
			MonitorId = NecDisplayCommand.MONITOR_ID_ALL;
		}

		#region Methods

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		public override void ConfigurePort(IPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new BoundedSerialBuffer(NecDisplayCommand.START_HEADER, NecDisplayCommand.END_MESSAGE);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port as ISerialPort);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;
			
			SetSerialQueue(queue);

			ISerialPort serialPort = port as ISerialPort;
			if (serialPort != null && serialPort.IsConnected)
				QueryState();
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			SendCommand(NecDisplayCommand.Command(MonitorId, s_PowerControl.Concat(s_PowerOn)));
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			SendCommand(NecDisplayCommand.Command(MonitorId, s_PowerControl.Concat(s_PowerOff)));
		}

		public override void SetActiveInput(int address)
		{
			SendCommand(NecDisplayCommand.SetParameterCommand(MonitorId, INPUT_PAGE, INPUT_CODE, s_InputMap[address]));
		}

		public override void VolumeUpIncrement()
		{
			if (!VolumeControlAvailable)
				return;
			SetVolume((ushort)(Volume + VOLUME_INCREMENT));
		}

		public override void VolumeDownIncrement()
		{
			if (!VolumeControlAvailable)
				return;
			SetVolume((ushort)(Volume - VOLUME_INCREMENT));
		}

		/// <summary>
		/// Enables mute.
		/// </summary>
		public override void MuteOn()
		{
			SendCommand(NecDisplayCommand.SetParameterCommand(MonitorId, MUTE_PAGE, MUTE_CODE, MUTE));
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public override void MuteOff()
		{
			SendCommand(NecDisplayCommand.SetParameterCommand(MonitorId, MUTE_PAGE, MUTE_CODE, UNMUTE));
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public override void VolumeRamp(bool increment, long timeout)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public override void VolumeRampStop()
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sends the volume set command to the device after validation has been performed.
		/// </summary>
		/// <param name="raw"></param>
		protected override void SetVolumeFinal(float raw)
		{
			if (!VolumeControlAvailable)
				return;

			ushort volume = (ushort)Math.Round(raw);
			SendCommand(NecDisplayCommand.SetParameterCommand(MonitorId, VOLUME_PAGE, VOLUME_CODE, volume), VolumeComparer);
		}

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Query the state of the device
			SendCommand(NecDisplayCommand.Command(MonitorId, s_PowerQuery));

			if (PowerState != ePowerState.PowerOn)
				return;

			SendCommand(NecDisplayCommand.GetParameterCommand(MonitorId, VOLUME_PAGE, VOLUME_CODE));
			SendCommand(NecDisplayCommand.GetParameterCommand(MonitorId, INPUT_PAGE, INPUT_CODE));
			SendCommand(NecDisplayCommand.GetParameterCommand(MonitorId, MUTE_PAGE, MUTE_CODE));
		}

		/// <summary>
		/// Prevents multiple volume commands from being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool VolumeComparer(ISerialData commandA, ISerialData commandB)
		{
			NecDisplayCommand necA = (NecDisplayCommand)commandA;
			NecDisplayCommand necB = (NecDisplayCommand)commandB;

			return necA.MessageType == necB.MessageType &&
				   necA.OpCodePage == necB.OpCodePage &&
				   necA.OpCode == necB.OpCode;
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

			NecDisplayCommand command = args.Data as NecDisplayCommand;
			if (command == null)
				return;

			throw new NotImplementedException();
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
			Logger.Log(eSeverity.Error, "Command {0} timed out.", args.Data.Serialize().Replace("\r", "\\r"));
		}

		/// <summary>
		/// For some reason the NEC specification uses "commands" instead of parameters for power,
		/// so the following is attempting to get the current power status from the replies.
		/// </summary>
		/// <param name="response"></param>
		private void ParseCommand(NecDisplayCommand response)
		{
			byte[] message = response.GetMessageWithoutStartEndCodes().ToArray();
			if (message.Length < 8)
				return;

			// Response to power query is prefixed with some unused data
			if (message[4] == 0x44 && message[5] == 0x36)
			{
				if (message[3] != 0x30)
				{
					ParseError(response);
					return;
				}

				PowerState = message[message.Length - 1] == 0x31 ? ePowerState.PowerOn : ePowerState.PowerOff;
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

			PowerState = message[message.Length - 1] == 0x31 ? ePowerState.PowerOn : ePowerState.PowerOff;

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

					if (page == INPUT_PAGE && code == INPUT_CODE)
					{
						ActiveInput = s_InputMap.ContainsValue(value)
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
			Logger.Log(eSeverity.Error, "Command {0} failed.", StringUtils.ToHexLiteral(args.Serialize()));
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

			settings.MonitorId = MonitorId;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			MonitorId = NecDisplayCommand.MONITOR_ID_ALL;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(NecDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			MonitorId = settings.MonitorId ?? NecDisplayCommand.MONITOR_ID_ALL;
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

			addRow("Monitor ID", MonitorId);
		}

		#endregion
	}
}
