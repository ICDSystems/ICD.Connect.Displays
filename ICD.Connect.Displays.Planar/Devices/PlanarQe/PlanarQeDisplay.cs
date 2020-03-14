using System;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Planar.Devices.PlanarQe
{
	public sealed class PlanarQeDisplay : AbstractDisplayWithAudio<PlanarQeDisplaySettings>
	{

		private const int PRIORITY_POWER_SET = 10;
		private const int PRIORITY_POWER_POLL = 20;
		private const int PRIORITY_INPUT_SET = 30;
		private const int PRIORITY_INPUT_POLL = 40;
		private const int PRIORITY_VOLUME_SET = 50;
		private const int PRIORITY_VOLUME_POLL = 60;

		#region Commands

		private const string COMMAND_POWER = "DISPLAY.POWER";
		private const string COMMAND_SOURCE = "SOURCE.SELECT";
		private const string COMMAND_VOLUME = "AUDIO.VOLUME";
		private const string COMMAND_MUTE = "AUDIO.MUTE";

		#endregion

		#region Operands

		private const string OPERAND_ON = "ON";
		private const string OPERAND_OFF = "OFF";

		#region Inputs

		// ReSharper disable IdentifierTypo
		private const string OPERAND_INPUT_HDMI_1 = "HDMI.1";
		private const string OPERAND_INPUT_HDMI_2 = "HDMI.2";
		private const string OPERAND_INPUT_HDMI_3 = "HDMI.3";
		private const string OPERAND_INPUT_HDMI_4 = "HDMI.4";
		private const string OPERAND_INPUT_DISPLAY_PORT = "DP";
		private const string OPERAND_INPUT_OPS = "OPS";
		// ReSharper restore IdentifierTypo

		#endregion
		#endregion


		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, OPERAND_INPUT_HDMI_1 },
			{2, OPERAND_INPUT_HDMI_2 },
			{3, OPERAND_INPUT_HDMI_3 },
			{4, OPERAND_INPUT_HDMI_4 },
			{5, OPERAND_INPUT_DISPLAY_PORT },
			{6, OPERAND_INPUT_OPS },
		};


		#region AbstractDisplayWithAudio Methods
		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_POWER, eCommandOperator.Set, OPERAND_ON), PRIORITY_POWER_SET);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_POWER, eCommandOperator.Set, OPERAND_OFF), PRIORITY_POWER_SET);
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public override void SetActiveInput(int address)
		{
			string operand;
			if (!s_InputMap.TryGetValue(address, out operand))
				throw new ArgumentOutOfRangeException("address", String.Format("{0} does not have an input at address {1}", this, address));

			SendCommandPriority(new PlanarQeCommand(COMMAND_SOURCE, eCommandOperator.Set, operand), PRIORITY_INPUT_SET);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetScalingMode(eScalingMode mode)
		{
			//This is no longer used, and will be removed from IDisplay soon(tm)
		}

		/// <summary>
		/// Called when a command is sent to the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialTransmission(object sender, SerialTransmissionEventArgs args)
		{
			// todo: Implement Trust Mode
			//throw new NotImplementedException();

			if (!Trust)
				return;

			PlanarQeCommand command = args.Data as PlanarQeCommand;

			if (command == null)
				return;

			switch (command.CommandCode)
			{
				case COMMAND_POWER:
					if (command.CommandOperator == eCommandOperator.Set)
						HandlePowerResponse(command);
					break;
				case COMMAND_MUTE:
					if (command.CommandOperator == eCommandOperator.Set)
						HandleMuteResponse(command);
					break;
				case COMMAND_SOURCE:
					if (command.CommandOperator == eCommandOperator.Set)
						HandleSourceResponse(command);
					break;
				case COMMAND_VOLUME:
					switch (command.CommandOperator)
					{
						case eCommandOperator.Set:
							HandleVolumeResponse(command);
							break;
						case eCommandOperator.Increment:
							Volume++;
							break;
						case eCommandOperator.Decrement:
							Volume--;
							break;
					}
					break;
			}
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			PlanarQeCommand response = PlanarQeCommand.ParseResponse(args.Response);

			if (response.CommandOperator == eCommandOperator.Err)
			{
				if (args.Data != null)
					Log(eSeverity.Error, "Error executing command: {0} - {1}", args.Data.Serialize(), args.Response);
				else
					Log(eSeverity.Error, "Error from device: {0}", args.Response);
				return;
			}

			if (response.CommandOperator == eCommandOperator.Response)
			{
				HandleResponese(response);
			}
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			//todo: Handled command timeouts
			//throw new NotImplementedException();
		}

		/// <summary>
		/// Increments the raw volume.
		/// </summary>
		public override void VolumeUpIncrement()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.Increment), PRIORITY_VOLUME_SET);
		}

		/// <summary>
		/// Decrements the raw volume.
		/// </summary>
		public override void VolumeDownIncrement()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.Decrement), PRIORITY_VOLUME_SET);
		}

		/// <summary>
		/// Sends the volume set command to the device after validation has been performed.
		/// </summary>
		/// <param name="raw"></param>
		protected override void VolumeSetRawFinal(float raw)
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.Set, raw.ToString("0")), PRIORITY_VOLUME_SET);
		}

		/// <summary>
		/// Enables mute.
		/// </summary>
		public override void MuteOn()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_MUTE, eCommandOperator.Set, OPERAND_ON),PRIORITY_VOLUME_SET );
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public override void MuteOff()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_MUTE, eCommandOperator.Set, OPERAND_OFF), PRIORITY_VOLUME_SET);
		}
		#endregion

		#region PrivateMethods

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			QueryPower();

			if (PowerState != ePowerState.PowerOn)
				return;

			QuerySource();
			QueryMute();
			QueryVolume();
		}

		private void QueryPower()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_POWER, eCommandOperator.GetName), PRIORITY_POWER_POLL );
		}

		private void QuerySource()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_SOURCE, eCommandOperator.GetName), PRIORITY_INPUT_POLL );
		}

		private void QueryMute()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_MUTE, eCommandOperator.GetName), PRIORITY_VOLUME_POLL);
		}

		private void QueryVolume()
		{
			SendCommandPriority(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.GetName), PRIORITY_VOLUME_POLL);
		}

		#region Response Handling

		private void HandleResponese(PlanarQeCommand response)
		{
			switch (response.CommandCode)
			{
				case COMMAND_POWER:
					HandlePowerResponse(response);
					break;
				case COMMAND_SOURCE:
					HandleSourceResponse(response);
					break;
				case COMMAND_MUTE:
					HandleMuteResponse(response);
					break;
				case COMMAND_VOLUME:
					HandleVolumeResponse(response);
					break;
			}
		}

		private void HandlePowerResponse(PlanarQeCommand response)
		{
			PowerState = GetBoolOperands(response.Operands) ? ePowerState.PowerOn : ePowerState.PowerOff;
		}

		private void HandleSourceResponse(PlanarQeCommand response)
		{
			if (response.Operands == null)
				throw new InvalidOperationException("Source response has no operands");

			if (response.Operands.Length < 1)
				throw new InvalidOperationException("Source response operands does not have a value");

			int address;
			if (s_InputMap.TryGetKey(response.Operands[0], out address))
				ActiveInput = address;
			else
				ActiveInput = null;
		}

		private void HandleMuteResponse(PlanarQeCommand response)
		{
			IsMuted = GetBoolOperands(response.Operands);
		}

		private void HandleVolumeResponse(PlanarQeCommand response)
		{
			if (response.Operands == null)
				throw new InvalidOperationException("Volume response has no operands");

			if (response.Operands.Length < 1)
				throw new InvalidOperationException("Value response operands does not have a value");

			int volume;
			try
			{
				volume = int.Parse(response.Operands[0]);
			}
			catch (FormatException e)
			{
				Log(eSeverity.Error, "Could not parse volume response as int: {0}-{1}", response.Operands[0], e.Message);
				return;
			}

			Volume = volume;
		}

		private bool GetBoolOperands(string[] operands)
		{
			if (operands == null)
				throw new ArgumentNullException("operands");

			if (operands.Length < 1)
				throw  new ArgumentOutOfRangeException("operands");

			return string.Equals(operands[0], OPERAND_ON, StringComparison.InvariantCultureIgnoreCase);
		}

		#endregion

		#endregion

		#region Device Communications

		private void SendCommandPriority(PlanarQeCommand command, int priority)
		{
			SendCommand(command, PlanarQeCommand.CommandComparer, priority);
		}

		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new DelimiterSerialBuffer(PlanarQeCommand.TERMINATOR);

			SerialQueue queue = new SerialQueue
			{
				Timeout = 2 * 1000
			};

			queue.SetBuffer(buffer);
			queue.SetPort(port);
			
			SetSerialQueue(queue);
		}

		#endregion
	}
}
