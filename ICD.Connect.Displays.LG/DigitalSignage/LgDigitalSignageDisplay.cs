using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.LG.DigitalSignage
{
	public sealed class LgDigitalSignageDisplay : AbstractDisplayWithAudio<LgDigitalSignageDisplaySettings>
	{
		/// <summary>
		/// Maps scaling mode to command.
		/// </summary>
		private static readonly BiDictionary<eScalingMode, string> s_ScalingModeMap =
			new BiDictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, "02"},
				{eScalingMode.Square4X3, "01"},
				{eScalingMode.NoScale, "09"},
				{eScalingMode.Zoom, "04"}
			};

		#region Properties

		public int SetId { get; set; }

		#endregion

		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			if (port != null)
			{
				port.DebugRx = eDebugMode.MixedAsciiHex;
				port.DebugTx = eDebugMode.MixedAsciiHex;
			}

			ISerialBuffer buffer = new DelimiterSerialBuffer((char)0x0D);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
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
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission
			{
				Command1 = 'k',
				Command2 = 'a',
				SetId = SetId,
				Data = "01"
			};

			SendCommand(command);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission
			{
				Command1 = 'k',
				Command2 = 'a',
				SetId = SetId,
				Data = "00"
			};

			SendCommand(command);
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public override void SetActiveInput(int address)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetScalingMode(eScalingMode mode)
		{
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission
			{
				Command1 = 'k',
				Command2 = 'c',
				SetId = SetId,
				Data = s_ScalingModeMap.GetValue(mode)
			};

			SendCommand(command);
		}

		/// <summary>
		/// Increments the raw volume.
		/// </summary>
		public override void VolumeUpIncrement()
		{
			VolumeSetRawFinal(Volume + 1);
		}

		/// <summary>
		/// Decrements the raw volume.
		/// </summary>
		public override void VolumeDownIncrement()
		{
			VolumeSetRawFinal(Volume - 1);
		}

		/// <summary>
		/// Sends the volume set command to the device after validation has been performed.
		/// </summary>
		/// <param name="raw"></param>
		protected override void VolumeSetRawFinal(float raw)
		{
			int volumeInt = (int)MathUtils.Clamp(raw, VolumeDeviceMin, VolumeDeviceMax);

			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission
			{
				Command1 = 'k',
				Command2 = 'f',
				SetId = SetId,
				Data = volumeInt.ToString("X2") // 2 digit hex (00-64)
			};

			SendCommand(command);
		}

		/// <summary>
		/// Enables mute.
		/// </summary>
		public override void MuteOn()
		{
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission
			{
				Command1 = 'k',
				Command2 = 'e',
				SetId = SetId,
				Data = "00"
			};

			SendCommand(command);
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public override void MuteOff()
		{
			LgDigitalSignageTransmission command = new LgDigitalSignageTransmission
			{
				Command1 = 'k',
				Command2 = 'e',
				SetId = SetId,
				Data = "01"
			};

			SendCommand(command);
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
			//IcdConsole.PrintLine(eConsoleColor.Magenta, "SerialQueueOnSerialTransmission {0}", args.Data);
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

			//IcdConsole.PrintLine(eConsoleColor.Magenta, "SerialQueueOnSerialResponse {0} - {1}", args.Data, args.Response);
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			IcdConsole.PrintLine(eConsoleColor.Magenta, "SerialQueueOnTimeout {0}", args.Data);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			/*
			// Query the state of the device
			SendCommand(new SamsungProCommand(POWER, WallId, 0).ToQuery());

			if (!IsPowered)
				return;

			SendCommand(new SamsungProCommand(VOLUME, WallId, 0).ToQuery());
			SendCommand(new SamsungProCommand(INPUT, WallId, 0).ToQuery());
			SendCommand(new SamsungProCommand(SCREEN_MODE, WallId, 0).ToQuery());
			SendCommand(new SamsungProCommand(MUTE, WallId, 0).ToQuery());
			 */
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
