using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public abstract class AbstractSamsungProDisplay<T> : AbstractDisplayWithAudio<T>, ISamsungProDisplay
		where T : ISamsungProDisplaySettings, new()
	{
		private const byte POWER = 0x11;
		private const byte VOLUME = 0x12;
		private const byte MUTE = 0x13;
		private const byte INPUT = 0x14;

		private const byte INPUT_HDMI_1 = 0x21;
		private const byte INPUT_HDMI_1_PC = 0x22;
		private const byte INPUT_HDMI_2 = 0x23;
		private const byte INPUT_HDMI_2_PC = 0x24;
		private const byte INPUT_HDMI_3 = 0x31;
		private const byte INPUT_HDMI_3_PC = 0x31;
		private const byte INPUT_DISPLAYPORT = 0x25;
		private const byte INPUT_DVI = 0x18;
		private const byte INPUT_DVI_VIDEO = 0x1F;

		private const ushort VOLUME_INCREMENT = 1;

		// ReSharper disable StaticFieldInGenericType
		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly BiDictionary<int, byte> s_InputMap = new BiDictionary<int, byte>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2},
			{3, INPUT_HDMI_3},
			{4, INPUT_DISPLAYPORT},
			{5, INPUT_DVI},
		};

		private static readonly BiDictionary<int, byte> s_InputPcMap = new BiDictionary<int, byte>
		{
			{1, INPUT_HDMI_1_PC},
			{2, INPUT_HDMI_2_PC},
			{3, INPUT_HDMI_3_PC},
			{5, INPUT_DVI_VIDEO}
		};
		// ReSharper restore StaticFieldInGenericType

		#region Properties

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

		#region Methods

		protected abstract byte GetWallIdForPowerCommand();

		protected abstract byte GetWallIdForInputCommand();

		protected abstract byte GetWallIdForVolumeCommand();

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new SamsungProDisplayBuffer();
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 3 * 1000;

			SetSerialQueue(queue);

			if (port != null && port.IsConnected)
				QueryState();
		}

		public override void PowerOn()
		{
			SendCommand(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 1));
		}

		public override void PowerOff()
		{
			SendCommand(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 0));
		}

		public override void SetActiveInput(int address)
		{
			SendCommand(new SamsungProCommand(INPUT, GetWallIdForInputCommand(), s_InputMap.GetValue(address)));
		}

		public override void MuteOn()
		{
			SendCommand(new SamsungProCommand(MUTE, GetWallIdForVolumeCommand(), 1));
		}

		public override void MuteOff()
		{
			SendCommand(new SamsungProCommand(MUTE, GetWallIdForVolumeCommand(), 0));
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

		protected override void SetVolumeFinal(float raw)
		{
			byte volume = (byte)Math.Round(raw);

			SendCommand(new SamsungProCommand(VOLUME, GetWallIdForVolumeCommand(), volume), CommandComparer);

			// Display unmutes on volume change, if and only if its currently muted
			if (IsMuted)
				SendCommand(new SamsungProCommand(MUTE, GetWallIdForVolumeCommand(), 0).ToQuery(), CommandComparer);
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
			if (PowerState != ePowerState.PowerOn)
				return;

			SetVolume((ushort)(Volume + VOLUME_INCREMENT));
		}

		public override void VolumeDownIncrement()
		{
			if (PowerState != ePowerState.PowerOn)
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
			SendCommandPriority(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 0).ToQuery(), int.MinValue);

			if (PowerState != ePowerState.PowerOn)
				return;

			SendCommandPriority(new SamsungProCommand(VOLUME, GetWallIdForVolumeCommand(), 0).ToQuery(), int.MinValue);
			SendCommandPriority(new SamsungProCommand(INPUT, GetWallIdForInputCommand(), 0).ToQuery(), int.MinValue);
			SendCommandPriority(new SamsungProCommand(MUTE, GetWallIdForVolumeCommand(), 0).ToQuery(), int.MinValue);
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

			SamsungProCommand command = args.Data as SamsungProCommand;
			if (command == null)
				return;

			switch (command.Command)
			{
				case POWER:
					PowerState = command.Data == 1 ? ePowerState.PowerOn : ePowerState.PowerOff;
					return;

				case VOLUME:
					Volume = command.Data;
					return;

				case MUTE:
					IsMuted = command.Data == 1;
					return;

				case INPUT:
					ActiveInput = s_InputMap.GetKey(command.Data);
					return;
			}
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			SamsungProResponse response = new SamsungProResponse(args.Response);

			switch (response.Header)
			{
				case 0xAA:
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
							SendCommandPriority(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 0).ToQuery(), 0);
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
			if (SerialQueue != null && SerialQueue.TimeoutCount < 10)
				SerialQueue.EnqueuePriority(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 0).ToQuery());
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
					if (response.Id != GetWallIdForPowerCommand())
						return;
					byte powerValue = response.Values[0];
					if (powerValue == 1)
						PowerState = ePowerState.PowerOn;
					else if (powerValue == 0)
						PowerState = ePowerState.PowerOff;
					return;

				case VOLUME:
					if (response.Id != GetWallIdForVolumeCommand())
						return;
					Volume = response.Values[0];
					return;

				case MUTE:
					if (response.Id != GetWallIdForVolumeCommand())
						return;
					byte muteValue = response.Values[0];
					if (muteValue == 1)
						IsMuted = true;
					else if (muteValue == 0)
						IsMuted = false;
					return;

				case INPUT:
					if (response.Id != GetWallIdForInputCommand())
						return;
					byte inputCode = response.Values[0];
					ActiveInput = s_InputMap.ContainsValue(inputCode)
									? s_InputMap.GetKey(inputCode)
									: s_InputPcMap.ContainsValue(inputCode)
										  ? s_InputPcMap.GetKey(inputCode)
										  : (int?)null;
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
	}
}
