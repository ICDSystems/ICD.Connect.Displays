using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
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

		#region Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		public override void ConfigurePort(IPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new DelimiterSerialBuffer(SonyBraviaCommand.FOOTER);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port as ISerialPort);
			queue.SetBuffer(buffer);
			queue.Timeout = 20 * 1000;

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
		protected override void SetVolumeFinal(float raw)
		{
			uint volume = (uint)Math.Round(raw);
			SonyBraviaCommand command = SonyBraviaCommand.Control(VOLUME_FUNCTION, volume.ToString());
			SendCommand(command, VolumeComparer);
		}

		/// <summary>
		/// Prevents multiple volume commands from being queued.
		/// </summary>
		/// <param name="dataA"></param>
		/// <param name="dataB"></param>
		/// <returns></returns>
		private static bool VolumeComparer(ISerialData dataA, ISerialData dataB)
		{
			SonyBraviaCommand commandA = (SonyBraviaCommand)dataA;
			SonyBraviaCommand commandB = (SonyBraviaCommand)dataB;

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

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Query the state of the device
			SendCommand(SonyBraviaCommand.Enquiry(POWER_FUNCTION));

			if (PowerState != ePowerState.PowerOn)
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
			SonyBraviaCommand command = args.Data as SonyBraviaCommand;
			SonyBraviaCommand response = SonyBraviaCommand.Response(args.Response);

			if (response.Parameter == SonyBraviaCommand.ERROR)
			{
				ParseError(args);
				return;
			}

			switch (response.Type)
			{
				// Command result
				case SonyBraviaCommand.eCommand.Answer:
					// This shouldn't happen
					if (command == null)
						break;

					switch (command.Type)
					{
						// Control result
						case SonyBraviaCommand.eCommand.Control:
							ParseControlResult(command, response);
							break;
						// Query result
						case SonyBraviaCommand.eCommand.Enquiry:
							ParseQuery(response);
							break;
					}
					break;

				// Event notification
				case SonyBraviaCommand.eCommand.Notify:
					ParseQuery(response);
					break;
			}
		}

		/// <summary>
		/// Parses the command result.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="response"></param>
// ReSharper disable UnusedParameter.Local
		private void ParseControlResult([NotNull] SonyBraviaCommand command, [NotNull] SonyBraviaCommand response)
// ReSharper restore UnusedParameter.Local
		{
			if (command == null)
				throw new ArgumentNullException("command");

			if (response == null)
				throw new ArgumentNullException("response");

			if (response.Parameter != SonyBraviaCommand.SUCCESS)
				throw new InvalidOperationException("Response does not represent a successful command");

			// Hack - Bravia uses the value "0" for success, so lets treat the command as the result
			ParseQuery(command);
		}

		/// <summary>
		/// Called when a query command is successful.
		/// </summary>
		/// <param name="response"></param>
		private void ParseQuery([NotNull] SonyBraviaCommand response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			switch (response.Function)
			{
				case POWER_FUNCTION:
					PowerState = int.Parse(response.Parameter) == 1 ? ePowerState.PowerOn : ePowerState.PowerOff;
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
				Logger.Log(eSeverity.Error, "Error - {0}", StringUtils.ToMixedReadableHexLiteral(args.Response));
			else
				Logger.Log(eSeverity.Error, "Command failed - {0}", StringUtils.ToMixedReadableHexLiteral(command.Serialize()));
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Logger.Log(eSeverity.Error, "Command timed out - {0}", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
		}
	}
}
