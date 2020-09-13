using System;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	/// <summary>
	/// SamsungDisplay provides methods for interacting with a Samsung EX Link TV
	/// </summary>
	public sealed class SamsungDisplay : AbstractDisplayWithAudio<SamsungDisplaySettings>
	{
		private const int MAX_RETRIES = 50;

		private const string RETURN = "\x03\x0C";

		public const string SUCCESS = RETURN + "\xF1";
		public const string FAILURE = RETURN + "\xFF";

		private const string COMMAND_PREFIX = "\x08\x22";

		private const string POWER_PREFIX = COMMAND_PREFIX + "\x00\x00\x00";
		private const string POWER_ON     = POWER_PREFIX + "\x02";
		private const string POWER_OFF    = POWER_PREFIX + "\x01";
		private const string POWER_TOGGLE = POWER_PREFIX + "\x00";

		private const string MUTE_PREFIX = COMMAND_PREFIX + "\x02\x00\x00";
		private const string MUTE_TOGGLE = MUTE_PREFIX + "\x00";
		private const string MUTE_ON     = MUTE_PREFIX + "\x01";
		private const string MUTE_OFF    = MUTE_PREFIX + "\x02";

		private const string VOLUME_PREFIX = COMMAND_PREFIX + "\x01\x00";
		private const string VOLUME        = VOLUME_PREFIX + "\x00";
		private const string VOLUME_UP     = VOLUME_PREFIX + "\x01\x00";
		private const string VOLUME_DOWN   = VOLUME_PREFIX + "\x02\x00";

		private const string INPUT_PREFIX = COMMAND_PREFIX + "\x0A\x00\x05";
		private const string INPUT_HDMI_1 = INPUT_PREFIX + "\x00";
		private const string INPUT_HDMI_2 = INPUT_PREFIX + "\x01";
		private const string INPUT_HDMI_3 = INPUT_PREFIX + "\x02";
		private const string INPUT_HDMI_4 = INPUT_PREFIX + "\x03";

		private const int PRIORITY_POWER_RETRY = 1;
		private const int PRIORITY_POWER_INITIAL = 2;
		private const int PRIORITY_INPUT_RETRY = 3;
		private const int PRIORITY_INPUT_INITIAL = 4;
		private const int PRIORITY_DEFAULT = int.MaxValue;

		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2},
			{3, INPUT_HDMI_3},
			{4, INPUT_HDMI_4}
		};

		private int m_PowerRetries;
		private int m_InputRetries;

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

			SamsungDisplaySerialBuffer buffer = new SamsungDisplaySerialBuffer();
			buffer.OnJunkData += BufferOnJunkData;

			SerialQueue queue = new SerialQueue
			{
				CommandDelayTime = 600,
				Timeout = 2500,
			};
			queue.SetPort(port as ISerialPort);
			queue.SetBuffer(buffer);
			queue.Timeout = 2500; // 2.5 Second Timeout

			SetSerialQueue(queue);
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		[PublicAPI]
		public override void PowerOn()
		{
			SendNonFormattedCommand(POWER_ON, CommandComparer, PRIORITY_POWER_INITIAL);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		[PublicAPI]
		public override void PowerOff()
		{
			if(SerialQueue == null)
				return;
			Logger.Log(eSeverity.Debug, "Display Power Off while {0} commands were enqueued. Commands dropped.", SerialQueue.CommandCount);
			SerialQueue.Clear();

			SendNonFormattedCommand(POWER_OFF, CommandComparer, PRIORITY_POWER_INITIAL);
		}

		[PublicAPI]
		public void PowerToggle()
		{
			SendNonFormattedCommand(POWER_TOGGLE, CommandComparer, PRIORITY_POWER_INITIAL);
		}

		public override void MuteOn()
		{
			SendNonFormattedCommand(MUTE_ON, CommandComparer);
		}

		public override void MuteOff()
		{
			SendNonFormattedCommand(MUTE_OFF, CommandComparer);
		}

		public override void MuteToggle()
		{
			SendNonFormattedCommand(MUTE_TOGGLE, CommandComparer);
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
			if (!VolumeControlAvailable)
				return;

			byte volume = (byte)Math.Round(raw);
			SendNonFormattedCommand(VOLUME + (char)volume, VolumeComparer);
		}

		/// <summary>
		/// Prevents multiple volume commands from being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool VolumeComparer(string commandA, string commandB)
		{
			return commandA.StartsWith(VOLUME, StringComparison.Ordinal) && commandB.StartsWith(VOLUME, StringComparison.Ordinal);
		}

		public override void VolumeUpIncrement()
		{
			if (!VolumeControlAvailable)
				return;

			SendNonFormattedCommand(VOLUME_UP);
		}

		public override void VolumeDownIncrement()
		{
			if (!VolumeControlAvailable)
				return;

			SendNonFormattedCommand(VOLUME_DOWN);
		}

		public override void SetActiveInput(int address)
		{
			SendNonFormattedCommand(s_InputMap.GetValue(address), CommandComparer, PRIORITY_INPUT_INITIAL);
		}

		/// <summary>
		/// Calculates the checksum for the data.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		public static string GetCheckSum(string data)
		{
			byte[] array = Encoding.ASCII.GetBytes(data);
			int sum = array.Sum(b => (int)b);
			int result = 0x100 - sum;

			return ((char)result).ToString();
		}

		/// <summary>
		/// Returns the first 6 characters of the command.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[PublicAPI]
		public static string RemoveCheckSum(string data)
		{
			return data.Substring(0, 6);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Calculates the checksum and queues the data to be sent to the physical display.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="priority"></param>
		private void SendNonFormattedCommand(string data, int priority)
		{
			SendNonFormattedCommand(data, (a, b) => false, priority);
		}

		/// <summary>
		/// Calculates the checksum and queues the data to be sent to the physical display.
		/// </summary>
		/// <param name="data"></param>
		private void SendNonFormattedCommand(string data)
		{
			SendNonFormattedCommand(data, PRIORITY_DEFAULT);
		}

		/// <summary>
		/// Calculates the checksum and queues the data to be sent to the physical display.
		/// Replaces an earlier command if found via the comparer.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="comparer"></param>
		private void SendNonFormattedCommand(string data, Func<string, string, bool> comparer)
		{
			SendNonFormattedCommand(data, comparer,PRIORITY_DEFAULT);
		}

		/// <summary>
		/// Calculates the checksum and queues the data to be sent to the physical display.
		/// Replaces an earlier command if found via the comparer.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="comparer"></param>
		/// <param name="priority"></param>
		private void SendNonFormattedCommand(string data, Func<string, string, bool> comparer, int priority)
		{
			data += GetCheckSum(data);

			SendCommand(new SerialData(data), (a, b) => comparer(a.Serialize(), b.Serialize()), priority);
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

			string command = RemoveCheckSum(args.Data.Serialize());

			switch (command)
			{
				case POWER_ON:
					PowerState = ePowerState.PowerOn;
					return;

				case POWER_OFF:
					PowerState = ePowerState.PowerOff;
					return;

				case POWER_TOGGLE:
					PowerState = PowerState == ePowerState.PowerOn ? ePowerState.PowerOff : ePowerState.PowerOn;
					return;

				case MUTE_ON:
					IsMuted = true;
					return;

				case MUTE_OFF:
					IsMuted = false;
					return;

				case MUTE_TOGGLE:
					IsMuted = !IsMuted;
					return;
			}

			if (command.StartsWith(VOLUME, StringComparison.Ordinal))
			{
				Volume = (byte)command[command.Length - 1];
				return;
			}

			if (s_InputMap.ContainsValue(command))
			{
				ActiveInput = s_InputMap.GetKey(command);
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
			if (args.Response.EndsWith(FAILURE))
				ParseError(args);
			else if (args.Response.EndsWith(SUCCESS))
				ParseSuccess(args);
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			var command = RemoveCheckSum(args.Data.Serialize());
			Logger.Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToHexLiteral(command));


			if (SerialQueue == null)
				return;

			// Re-queue power on or input select commands that fail
			if (command == POWER_ON)
			{
				m_PowerRetries++;
				if (m_PowerRetries > MAX_RETRIES)
				{
					Logger.Log(eSeverity.Error, "Power On Command for Samsung Display Reached Max Retries, aborting.");
					m_PowerRetries = 0;
					return;
				}
				SerialQueue.EnqueuePriority(new SerialData(args.Data.Serialize()), PRIORITY_POWER_RETRY);
			}
			else if (s_InputMap.ContainsValue(command))
			{
				m_InputRetries++;

				// If input commands hit a specified limit, enqueue a power on command at higher priority to make sure the display is actually powered on)
				if (m_InputRetries > MAX_RETRIES / 2)
				{
					SendNonFormattedCommand(POWER_ON, CommandComparer, PRIORITY_POWER_RETRY);
				}

				if (m_InputRetries > MAX_RETRIES)
				{
					Logger.Log(eSeverity.Error, "Input Command for Samsung Display Reached Max Retries, aborting.");
					m_InputRetries = 0;
					return;
				}
				SerialQueue.EnqueuePriority(args.Data, PRIORITY_INPUT_RETRY);
			}
		}

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="args"></param>
		private void ParseSuccess(SerialResponseEventArgs args)
		{
			string command = RemoveCheckSum(args.Data.Serialize());

			// HDMI
			if (s_InputMap.Values.Contains(command))
			{
				PowerState = ePowerState.PowerOn;
				ActiveInput = s_InputMap.ContainsValue(command)
					            ? s_InputMap.GetKey(command)
					            : (int?)null;

				m_InputRetries = 0;

				return;
			}

			// Volume
			if (command.StartsWith(VOLUME, StringComparison.Ordinal))
			{
				PowerState = ePowerState.PowerOn;
				Volume = command[5];
				IsMuted = false;
				return;
			}

			switch (command)
			{
				case POWER_ON:
					PowerState = ePowerState.PowerOn;
					m_PowerRetries = 0;
					return;
				case POWER_OFF:
					PowerState = ePowerState.PowerOff;
					m_PowerRetries = 0;
					return;
				case MUTE_ON:
					PowerState = ePowerState.PowerOn;
					IsMuted = true;
					return;
				case MUTE_OFF:
					PowerState = ePowerState.PowerOn;
					IsMuted = false;
					return;
			}
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			string command = StringUtils.ToHexLiteral(args.Data.Serialize());

			Logger.Log(eSeverity.Error, "Command {0} failed.", command);
		}

		private void BufferOnJunkData(object sender, EventArgs eventArgs)
		{
			//IsPowered = true;
		}

		/// <summary>
		/// Prevents multiple volume commands being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool CommandComparer(string commandA, string commandB)
		{
			if (commandA.StartsWith(POWER_PREFIX, StringComparison.Ordinal) && commandB.StartsWith(POWER_PREFIX, StringComparison.Ordinal))
				return true;

			if (commandA.StartsWith(MUTE_PREFIX, StringComparison.Ordinal) && commandB.StartsWith(MUTE_PREFIX, StringComparison.Ordinal))
				return true;

			if (commandA.StartsWith(INPUT_PREFIX, StringComparison.Ordinal) && commandB.StartsWith(INPUT_PREFIX, StringComparison.Ordinal))
				return true;

			return false;
		}

		#endregion
	}
}
