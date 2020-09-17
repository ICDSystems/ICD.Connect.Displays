using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
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

namespace ICD.Connect.Displays.LG.DigitalSignage
{
	public sealed class LgDigitalSignageDisplay : AbstractDisplayWithAudio<LgDigitalSignageDisplaySettings>
	{
		#region Constants

		private const string COMMAND_POWER = "ka";
		private const string COMMAND_INPUT = "xb";
		private const string COMMAND_VOLUME = "kf";
		private const string COMMAND_MUTE = "ke";

		private const string DATA_INPUT_AV = "20";
		private const string DATA_INPUT_COMPONENT = "40";
		private const string DATA_INPUT_RGB = "60";
		private const string DATA_INPUT_DVI_D_PC = "70";
		private const string DATA_INPUT_DVI_D_DTV = "80";
		private const string DATA_INPUT_HDMI1_DTV = "90";
		private const string DATA_INPUT_HDMI1_PC = "A0";
		private const string DATA_INPUT_HDMI2_OPS_DTV = "91";
		private const string DATA_INPUT_HDMI2_OPS_PC = "A1";
		private const string DATA_INPUT_OPS_HDMI3_DVI_D_DTV = "92";
		private const string DATA_INPUT_OPS_HDMI3_DVI_D_PC = "A2";
		private const string DATA_INPUT_OPS_DVI_D_DTV = "95";
		private const string DATA_INPUT_OPS_DVI_D_PC = "A5";
		private const string DATA_INPUT_HDMI3_DVI_D_DTV = "96";
		private const string DATA_INPUT_HDMI3_DVI_D_PC = "A6";
		private const string DATA_INPUT_DISPLAYPORT_DTV = "C0";
		private const string DATA_INPUT_DISPLAYPORT_PC = "D0";
		private const string DATA_INPUT_SUPERSIGN_WEB_OS_PLAYER = "E0";
		private const string DATA_INPUT_OTHERS = "E1";
		private const string DATA_INPUT_MULTI_SCREEN = "E2";

		private const string DATA_QUERY = "FF";

		#endregion

		private static readonly BiDictionary<int, string> s_InputMap =
			new BiDictionary<int, string>
			{
				{1, DATA_INPUT_HDMI1_PC},
				{2, DATA_INPUT_HDMI2_OPS_PC},
				{3, DATA_INPUT_HDMI3_DVI_D_PC},
			};

		#region Properties

		public int SetId { get; set; }

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

		public override void ConfigurePort(IPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new LgDigitalSignageSerialBuffer();
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port as ISerialPort);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);
		}

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission(COMMAND_POWER, SetId, "01");
			SendCommand(command);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission(COMMAND_POWER, SetId, "00");
			SendCommand(command);
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public override void SetActiveInput(int address)
		{
			string data = s_InputMap.GetValue(address);

			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission(COMMAND_INPUT, SetId, data);
			SendCommand(command);
		}

		/// <summary>
		/// Increments the raw volume.
		/// </summary>
		public override void VolumeUpIncrement()
		{
			SetVolumeFinal(Volume + 1);
		}

		/// <summary>
		/// Decrements the raw volume.
		/// </summary>
		public override void VolumeDownIncrement()
		{
			SetVolumeFinal(Volume - 1);
		}

		/// <summary>
		/// Sends the volume set command to the device after validation has been performed.
		/// </summary>
		/// <param name="raw"></param>
		protected override void SetVolumeFinal(float raw)
		{
			int volumeInt = (int)MathUtils.Clamp(Math.Round(raw), VolumeDeviceMin, VolumeDeviceMax);
			string data = volumeInt.ToString("X2"); // 2 digit hex (00-64)

			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission(COMMAND_VOLUME, SetId, data);
			SendCommand(command);
		}

		/// <summary>
		/// Enables mute.
		/// </summary>
		public override void MuteOn()
		{
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission(COMMAND_MUTE, SetId, "00");
			SendCommand(command);
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public override void MuteOff()
		{
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission(COMMAND_MUTE, SetId, "01");
			SendCommand(command);
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

		#region Serial Queue Callbacks

		/// <summary>
		/// Called when a command is sent to the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialTransmission(object sender, SerialTransmissionEventArgs args)
		{
			if (Trust)
				ParseSuccess(args.Data as LgDigitalSignageTransmission);
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			LgDigitalSignageAcknowledgement acknowledgement;
			if (!LgDigitalSignageAcknowledgement.Deserialize(args.Response, out acknowledgement))
				return;

			switch (acknowledgement.Ack)
			{
				case LgDigitalSignageAcknowledgement.eAck.Ok:
					LgDigitalSignageTransmission data = args.Data as LgDigitalSignageTransmission;
					if (data == null)
						return;

					// Hack - Replace query command with result
					if (data.Data == DATA_QUERY)
						data = new LgDigitalSignageTransmission(data.Command, data.SetId, acknowledgement.Data.ToUpper());

					ParseSuccess(data);
					break;

				case LgDigitalSignageAcknowledgement.eAck.Ng:
					ParseError(args.Data);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ParseSuccess(LgDigitalSignageTransmission data)
		{
			if (data.Data == DATA_QUERY)
				return;

			switch (data.Command)
			{
				case COMMAND_POWER:
					PowerState = data.Data == "01" ? ePowerState.PowerOn : ePowerState.PowerOff;
					break;

				case COMMAND_INPUT:
					int input;
					ActiveInput = s_InputMap.TryGetKey(data.Data, out input) ? input : (int?)null;
					break;

				case COMMAND_VOLUME:
					Volume = int.Parse(data.Data, System.Globalization.NumberStyles.HexNumber);
					break;

				case COMMAND_MUTE:
					IsMuted = data.Data == "00";
					break;
			}
		}

		private void ParseError(ISerialData data)
		{
			Logger.Log(eSeverity.Error, "Command {0} failed.", StringUtils.ToMixedReadableHexLiteral(data.Serialize()));
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Logger.Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Query the state of the device
			SendCommand(new LgDigitalSignageTransmission(COMMAND_POWER, SetId, DATA_QUERY));

			if (PowerState != ePowerState.PowerOn)
				return;

			SendCommand(new LgDigitalSignageTransmission(COMMAND_VOLUME, SetId, DATA_QUERY));
			SendCommand(new LgDigitalSignageTransmission(COMMAND_INPUT, SetId, DATA_QUERY));
			SendCommand(new LgDigitalSignageTransmission(COMMAND_MUTE, SetId, DATA_QUERY));
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetId = 1;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(LgDigitalSignageDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.SetId = SetId;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(LgDigitalSignageDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			SetId = settings.SetId;
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

			addRow("Set ID", SetId);
		}

		#endregion
	}
}
