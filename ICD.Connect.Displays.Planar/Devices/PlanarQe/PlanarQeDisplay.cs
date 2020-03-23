using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
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

		/// <summary>
		/// Keeps track of command retry counts
		/// </summary>
		private readonly Dictionary<string, int> m_CommandRetries;

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

		/// <summary>
		/// Constructor
		/// </summary>
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

			if (command.CommandOperator == eCommandOperator.Set)
			{
				HandleResponese(command);
				return;
			}

			// Handle volume increment/decrement
			if (command.CommandCode != COMMAND_VOLUME)
				return;

			switch (command.CommandOperator)
			{
				case eCommandOperator.Increment:
					Volume++;
					return;
				case eCommandOperator.Decrement:
					Volume--;
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
			PlanarQeCommand response = PlanarQeCommand.ParseCommand(args.Response);

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

			RetryCommand(command);
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
		protected override void SetVolumeFinal(float raw)
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

		#region PrivateMethods

		/// <summary>
		/// Sends the given command
		/// Uses the command comparer to collapse commands
		/// Uses the appropriate priority for the command
		/// </summary>
		/// <param name="command"></param>
		private void SendCommand([NotNull] PlanarQeCommand command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			SendCommand(command, PlanarQeCommand.CommandComparer, GetPriorityForCommand(command));
		}

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

		/// <summary>
		/// Polls the power state of the display
		/// </summary>
		private void QueryPower()
		{
			SendCommand(new PlanarQeCommand(COMMAND_POWER, eCommandOperator.GetName));
		}

		/// <summary>
		/// Polls the source state of the display
		/// </summary>
		private void QuerySource()
		{
			SendCommand(new PlanarQeCommand(COMMAND_SOURCE, eCommandOperator.GetName));
		}

		/// <summary>
		/// Polls the mute state of the display
		/// </summary>
		private void QueryMute()
		{
			SendCommand(new PlanarQeCommand(COMMAND_MUTE, eCommandOperator.GetName));
		}

		/// <summary>
		/// Polls the volume state of the display
		/// </summary>
		private void QueryVolume()
		{
			SendCommand(new PlanarQeCommand(COMMAND_VOLUME, eCommandOperator.GetName));
		}

		/// <summary>
		/// Retries the command if it's been sent less than the retry limit
		/// </summary>
		/// <param name="command"></param>
		private void RetryCommand([CanBeNull] PlanarQeCommand command)
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

		/// <summary>
		/// Resets the retry counter to 0 for the given command
		/// </summary>
		/// <param name="command"></param>
		private void ResetCommandRetry([NotNull] PlanarQeCommand command)
		{
			if (command == null)
				throw new ArgumentNullException("command");
			if (m_CommandRetries.ContainsKey(command.CommandCode))
				m_CommandRetries[command.CommandCode] = 0;
		}

		#region Response Handling

		/// <summary>
		/// Handle commands from the display with the response operator
		/// Also handles set commands sent to the display when in Trust mode
		/// </summary>
		/// <param name="response"></param>
		private void HandleResponese([NotNull] PlanarQeCommand response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

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

		/// <summary>
		/// Handles power responses, and sets the power state appropriately
		/// Also handles set commands sent to the display when in Trust mode
		/// </summary>
		/// <param name="response"></param>
		private void HandlePowerResponse([NotNull] PlanarQeCommand response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			PowerState = GetBoolOperands(response.Operands) ? ePowerState.PowerOn : ePowerState.PowerOff;
		}

		/// <summary>
		/// Handles source select responses, and sets the active input appropriately
		/// Also handles set commands sent to the display when in Trust mode
		/// </summary>
		/// <param name="response"></param>
		private void HandleSourceResponse([NotNull] PlanarQeCommand response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

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

		/// <summary>
		/// Handles mute responses, and sets the IsMuted state appropriately
		/// Also handles set commands sent to the display when in Trust mode
		/// </summary>
		/// <param name="response"></param>
		private void HandleMuteResponse([NotNull] PlanarQeCommand response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			IsMuted = GetBoolOperands(response.Operands);
		}

		/// <summary>
		/// Handles volume responses, and sets the volume level appropriately
		/// Also handles set commands sent to the display when in Trust mode
		/// </summary>
		/// <param name="response"></param>
		private void HandleVolumeResponse([NotNull] PlanarQeCommand response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

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

		#endregion

		#endregion

		#region Device Communications

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

		/// <summary>
		/// Gets the correct priority for the given command
		/// Based on the Command Code and Command Operator
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		[PublicAPI]
		public static int GetPriorityForCommand([NotNull] PlanarQeCommand command)
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

		/// <summary>
		/// Parses ON/OFF operand into a bool
		/// </summary>
		/// <param name="operands"></param>
		/// <returns></returns>
		[PublicAPI]
		public static bool GetBoolOperands([NotNull] IList<string> operands)
		{
			if (operands == null)
				throw new ArgumentNullException("operands");

			if (operands.Count < 1)
				throw  new ArgumentOutOfRangeException("operands");

			return string.Equals(operands[0], OPERAND_ON, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
