using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Sony
{
	public sealed class SonyBraviaDisplay : AbstractDisplayWithAudio<SonyBraviaDisplaySettings>
	{
		private const string POWER_FUNCTION = "POWR";
		private const string VOLUME_FUNCTION = "VOLU";
		private const string MUTE_FUNCTION = "AMUT";
		private const string INPUT_FUNCTION = "INPT";
		private const string IRCODE_FUNCTION = "IRCC";

		#region Properties

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		public override int InputCount { get { return 4; } }

		#endregion

		#region Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		protected override void ConfigurePort(ISerialPort port)
		{
			IComPort comPort = port as IComPort;
			if (comPort != null)
				ConfigureComPort(comPort);

			ISerialBuffer buffer = new DelimiterSerialBuffer(SonyBraviaCommand.FOOTER);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 20 * 1000;

			SetSerialQueue(queue);

			if (port != null && port.IsConnected)
				QueryState();
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

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			SonyBraviaCommand command = SonyBraviaCommand.Control(POWER_FUNCTION, "1");
			SendCommand(command);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			SonyBraviaCommand command = SonyBraviaCommand.Control(POWER_FUNCTION, "0");
			SendCommand(command);
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public override void SetActiveInput(int address)
		{
			string parameter = SonyBraviaCommand.SetHdmiInputParameter(address);
			SonyBraviaCommand command = SonyBraviaCommand.Control(INPUT_FUNCTION, parameter);

			SendCommand(command);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetScalingMode(eScalingMode mode)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Increments the raw volume.
		/// </summary>
		public override void VolumeUpIncrement()
		{
			SonyBraviaCommand command = SonyBraviaCommand.Control(IRCODE_FUNCTION, "30");
			SendCommand(command);

			command = SonyBraviaCommand.Enquiry(VOLUME_FUNCTION);
			SendCommand(command);
		}

		/// <summary>
		/// Decrements the raw volume.
		/// </summary>
		public override void VolumeDownIncrement()
		{
			SonyBraviaCommand command = SonyBraviaCommand.Control(IRCODE_FUNCTION, "31");
			SendCommand(command);

			command = SonyBraviaCommand.Enquiry(VOLUME_FUNCTION);
			SendCommand(command);
		}

		/// <summary>
		/// Sends the volume set command to the device after validation has been performed.
		/// </summary>
		/// <param name="raw"></param>
		protected override void VolumeSetRawFinal(float raw)
		{
			SonyBraviaCommand command = SonyBraviaCommand.Control(VOLUME_FUNCTION, ((uint)raw).ToString());
			SendCommand(command, VolumeComparer);
		}

		/// <summary>
		/// Prevents multiple volume commands from being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool VolumeComparer(SonyBraviaCommand commandA, SonyBraviaCommand commandB)
		{
			return commandA.Function == commandB.Function &&
				   commandA.Type == commandB.Type &&
				   commandA.Parameter == commandB.Parameter;
		}

		/// <summary>
		/// Enables mute.
		/// </summary>
		public override void MuteOn()
		{
			SonyBraviaCommand command = SonyBraviaCommand.Control(MUTE_FUNCTION, "1");
			SendCommand(command);
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public override void MuteOff()
		{
			SonyBraviaCommand command = SonyBraviaCommand.Control(MUTE_FUNCTION, "0");
			SendCommand(command);
		}

		#endregion

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Query the state of the device
			SendCommand(SonyBraviaCommand.Enquiry(POWER_FUNCTION));

			if (!IsPowered)
				return;

			SendCommand(SonyBraviaCommand.Enquiry(VOLUME_FUNCTION));
			SendCommand(SonyBraviaCommand.Enquiry(INPUT_FUNCTION));
			SendCommand(SonyBraviaCommand.Enquiry(MUTE_FUNCTION));
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

			SonyBraviaCommand command = args.Data as SonyBraviaCommand;
			if (command == null)
				return;

			// Hack - Treat the command like a response
			if (command.Type == SonyBraviaCommand.eCommand.Control)
				ParseQuery(command);
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			SonyBraviaCommand response = SonyBraviaCommand.Response(args.Response);

			if (response.Parameter == SonyBraviaCommand.ERROR)
				ParseError(args);
			else if (response.Type == SonyBraviaCommand.eCommand.Answer)
				ParseQuery(response);
		}

		/// <summary>
		/// Called when a query command is successful.
		/// </summary>
		/// <param name="response"></param>
		private void ParseQuery(SonyBraviaCommand response)
		{
			switch (response.Function)
			{
				case POWER_FUNCTION:
					IsPowered = int.Parse(response.Parameter) == 1;
					break;

				case VOLUME_FUNCTION:
					Volume = float.Parse(response.Parameter);
					break;

				case MUTE_FUNCTION:
					IsMuted = int.Parse(response.Parameter) == 1;
					break;

				case INPUT_FUNCTION:
					ActiveInput = SonyBraviaCommand.GetHdmiInputParameter(response.Parameter);
					break;
			}
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			SonyBraviaCommand command = args.Data as SonyBraviaCommand;

			if (command == null)
				Log(eSeverity.Error, "Error - {0}", StringUtils.ToMixedReadableHexLiteral(args.Response));
			else
				Log(eSeverity.Error, "Command failed - {0}", StringUtils.ToMixedReadableHexLiteral(command.Serialize()));
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Log(eSeverity.Error, "Command timed out - {0}", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
		}
	}
}
