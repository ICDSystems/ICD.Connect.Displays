using System;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Sony
{
	public sealed class SonyBraviaDisplay : AbstractDisplayWithAudio<SonyBraviaDisplaySettings>
	{
		private const string POWER_FUNCTION = "POWER";
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
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);

			ISerialBuffer buffer = new DelimiterSerialBuffer(SonyBraviaCommand.FOOTER);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);

			if (port != null)
				SendCommand(SonyBraviaCommand.Enquiry(POWER_FUNCTION));
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
			SonyBraviaCommand command = SonyBraviaCommand.Control(POWER_FUNCTION, "1");
			SendCommand(command);
		}

		public override void PowerOff()
		{
			SonyBraviaCommand command = SonyBraviaCommand.Control(POWER_FUNCTION, "2");
			SendCommand(command);
		}

		public override void SetHdmiInput(int address)
		{
			string parameter = SonyBraviaCommand.SetHdmiInputParameter(address);
			SonyBraviaCommand command = SonyBraviaCommand.Control(INPUT_FUNCTION, parameter);

			SendCommand(command);
		}

		public override void SetScalingMode(eScalingMode mode)
		{
			throw new NotImplementedException();
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
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			throw new NotImplementedException();
			/*
			SonyBraviaCommand request 
			SonyBraviaCommand response = new SonyBraviaCommand(args.Response);

			if (response.Parameter == SonyBraviaCommand.ERROR)
				ParseError(args);
			else if ((args.Data as SonyBraviaCommand).Type == SonyBraviaCommand.TYPE_ENQUIRY && response.Type == SonyBraviaCommand.TYPE_ANSWER)
				ParseQuery(args);
			*/
		}

		/// <summary>
		/// Called when a query command is successful.
		/// </summary>
		/// <param name="response"></param>
		private void ParseResponse(SonyBraviaCommand response)
		{
			/*
			switch (response.Function)
			{
				case POWER_FUNCTION:
					IsPowered = int.Parse(response.Parameter) == 1;
					break;

				case VOLUME_FUNCTION:
					Volume = (ushort)responseValue;
					break;

				case MUTE_FUNCTION:
					IsMuted = responseValue == 1;
					break;

				case INPUT_FUNCTION:
					HdmiInput = responseValue;
					break;

				case SCALING_MODE_QUERY:
					if (s_ViewModeMap.ContainsKey(responseValue))
					{
						string command = s_ViewModeMap[responseValue];
						ScalingMode = s_ScalingModeMap.GetKey(command);
					}
					else
						ScalingMode = eScalingMode.Unknown;
					break;
			}
			 */
		}

		/// <summary>
		///     Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			Log(eSeverity.Error, "Command failed - {0}", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
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

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SonyBraviaDisplaySettings settings)
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
		protected override void ApplySettingsFinal(SonyBraviaDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					IcdErrorLog.Error("No Serial Port with id {0}", settings.Port);
			}

			SetPort(port);
		}

		#endregion
	}
}
