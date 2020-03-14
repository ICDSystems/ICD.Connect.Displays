using System;
using System.Collections.Generic;
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

		private const int MAX_RETRIES = 50;

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

		private readonly Dictionary<string, int> m_CommandRetries;


		public PlanarQeDisplay()
		{
			m_CommandRetries = new Dictionary<string, int>();
		}


		#region AbstractDisplayWithAudio Methods
		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			SendCommand(new PlanarQeCommand(COMMAND_POWER, eCommandOperator.Set, OPERAND_ON));
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			SendCommand(new PlanarQeCommand(COMMAND_POWER, eCommandOperator.Set, OPERAND_OFF));
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

			SendCommand(new PlanarQeCommand(COMMAND_SOURCE, eCommandOperator.Set, operand));
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

			if (response == null)
			{
				Log(eSeverity.Error, "Unable to parse display response: {0}", args.Response);
				return;
			}

			switch (response.CommandOperator)
			{
				case eCommandOperator.Response:
					HandleResponese(response);
					break;
				case eCommandOperator.Err:
					if (args.Data != null)
					{
						Log(eSeverity.Error, "Error executing command, retrying: {0} - {1}", args.Data.Serialize(), args.Response);
						RetryCommand(args.Data as PlanarQeCommand);
					}
					else
						Log(eSeverity.Error, "Error from device: {0}", args.Response);
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
			Log(eSeverity.Informational, "Command timeout: {0}", args.Data.Serialize());

			PlanarQeCommand command = args.Data as PlanarQeCommand;

			if (command == null)
				return;

			
		}

		/// <summary>
		/// Increments the raw volume.
		/// </summary>
		public override void VolumeUpIncrement()
		{
			SendCommand(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.Increment));
		}

		/// <summary>
		/// Decrements the raw volume.
		/// </summary>
		public override void VolumeDownIncrement()
		{
			SendCommand(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.Decrement));
		}

		/// <summary>
		/// Sends the volume set command to the device after validation has been performed.
		/// </summary>
		/// <param name="raw"></param>
		protected override void VolumeSetRawFinal(float raw)
		{
			SendCommand(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.Set, raw.ToString("0")));
		}

		/// <summary>
		/// Enables mute.
		/// </summary>
		public override void MuteOn()
		{
			SendCommand(new PlanarQeCommand(COMMAND_MUTE, eCommandOperator.Set, OPERAND_ON));
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public override void MuteOff()
		{
			SendCommand(new PlanarQeCommand(COMMAND_MUTE, eCommandOperator.Set, OPERAND_OFF));
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
			SendCommand(new PlanarQeCommand(COMMAND_POWER, eCommandOperator.GetName));
		}

		private void QuerySource()
		{
			SendCommand(new PlanarQeCommand(COMMAND_SOURCE, eCommandOperator.GetName));
		}

		private void QueryMute()
		{
			SendCommand(new PlanarQeCommand(COMMAND_MUTE, eCommandOperator.GetName));
		}

		private void QueryVolume()
		{
			SendCommand(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.GetName));
		}

		private void RetryCommand(PlanarQeCommand command)
		{
			if (command == null)
				return;

			int count;

			if (!m_CommandRetries.TryGetValue(command.CommandCode, out count))
				count = 0;

			if (count >= MAX_RETRIES)
			{
				Log(eSeverity.Error, "Command hit timeout retry limit:{0}", command.Serialize());
				return;
			}

			Log(eSeverity.Debug, "Retrying command try {0}: {1}", count, command.Serialize());

			m_CommandRetries[command.CommandCode] = count + 1;

			SendCommand(command);
		}

		private void ResetCommandRetry(PlanarQeCommand command)
		{
			if (m_CommandRetries.ContainsKey(command.CommandCode))
				m_CommandRetries[command.CommandCode] = 0;
		}

		#region Response Handling

		private void HandleResponese(PlanarQeCommand response)
		{
			ResetCommandRetry(response);
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

		[Obsolete("obsolete", true)]
		private void SendCommand(PlanarQeCommand command, int priority)
		{
			SendCommand(command, PlanarQeCommand.CommandComparer, GetPriorityForCommand(command));
		}

		private void SendCommand(PlanarQeCommand command)
		{
			SendCommand(command, PlanarQeCommand.CommandComparer, GetPriorityForCommand(command));
		}

		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new DelimiterSerialBuffer(PlanarQeCommand.TERMINATOR);

			SerialQueue queue = new SerialQueue
			{
				Timeout = 5 * 1000
			};

			queue.SetBuffer(buffer);
			queue.SetPort(port);
			
			SetSerialQueue(queue);
		}

		#endregion

		public static int GetPriorityForCommand(PlanarQeCommand command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			switch (command.CommandCode)
			{
				case COMMAND_POWER:
					switch (command.CommandOperator)
					{
						case eCommandOperator.Set:
						case eCommandOperator.Increment:
						case eCommandOperator.Decrement:
							return PRIORITY_POWER_SET;
						case eCommandOperator.GetName:
						case eCommandOperator.GetNumeric:
							return PRIORITY_POWER_POLL;
					}

					break;
				case COMMAND_SOURCE:
					switch (command.CommandOperator)
					{
						case eCommandOperator.Set:
						case eCommandOperator.Increment:
						case eCommandOperator.Decrement:
							return PRIORITY_INPUT_SET;
						case eCommandOperator.GetName:
						case eCommandOperator.GetNumeric:
							return PRIORITY_INPUT_POLL;
					}

					break;
				case COMMAND_MUTE:
				case COMMAND_VOLUME:
					switch (command.CommandOperator)
					{
						case eCommandOperator.Set:
						case eCommandOperator.Increment:
						case eCommandOperator.Decrement:
							return PRIORITY_VOLUME_SET;
						case eCommandOperator.GetName:
						case eCommandOperator.GetNumeric:
							return PRIORITY_VOLUME_POLL;
					}

					break;
			}

			return int.MaxValue;
		}
	}
}
