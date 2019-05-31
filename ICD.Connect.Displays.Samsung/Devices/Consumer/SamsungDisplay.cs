using System;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
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

		private const string ASPECT_PREFIX     = COMMAND_PREFIX + "\x0B\x0A\x01";
		private const string ASPECT_16_X9      = ASPECT_PREFIX + "\x00";
		private const string ASPECT_ZOOM_1     = ASPECT_PREFIX + "\x01";
		private const string ASPECT_4_X3       = ASPECT_PREFIX + "\x04";
		private const string ASPECT_SCREEN_FIT = ASPECT_PREFIX + "\x05";

		private const int PRIORITY_POWER_RETRY = 1;
		private const int PRIORITY_POWER_INITIAL = 2;
		private const int PRIORITY_INPUT_RETRY = 3;
		private const int PRIORITY_INPUT_INITIAL = 4;
		private const int PRIORITY_DEFAULT = int.MaxValue;


		/// <summary>
		/// Maps scaling mode to command.
		/// </summary>
		private static readonly BiDictionary<eScalingMode, string> s_ScalingModeMap =
			new BiDictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, ASPECT_16_X9},
				{eScalingMode.Square4X3, ASPECT_4_X3},
				{eScalingMode.NoScale, ASPECT_SCREEN_FIT},
				{eScalingMode.Zoom, ASPECT_ZOOM_1}
			};

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

		#region Methods

		/// <summary>
		/// Sets and configures the port for communication with the physical display.
		/// </summary>
		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			SamsungDisplaySerialBuffer buffer = new SamsungDisplaySerialBuffer();
			buffer.OnJunkData += BufferOnJunkData;

			RateLimitedQueue queue = new RateLimitedQueue(600);
			queue.SetPort(port);
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
			Log(eSeverity.Debug, "Display Power Off while {0} commands were enqueued. Commands dropped.", SerialQueue.CommandCount);
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

		protected override void VolumeSetRawFinal(float raw)
		{
			if (!IsPowered)
				return;

			SendNonFormattedCommand(VOLUME + (char)(byte)raw, VolumeComparer);
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
			if (!IsPowered)
				return;

			SendNonFormattedCommand(VOLUME_UP);
		}

		public override void VolumeDownIncrement()
		{
			if (!IsPowered)
				return;

			SendNonFormattedCommand(VOLUME_DOWN);
		}

		public override void SetActiveInput(int address)
		{
			SendNonFormattedCommand(s_InputMap.GetValue(address), CommandComparer, PRIORITY_INPUT_INITIAL);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"/>
		public override void SetScalingMode(eScalingMode mode)
		{
			SendNonFormattedCommand(s_ScalingModeMap.GetValue(mode), CommandComparer);
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
					IsPowered = true;
					return;

				case POWER_OFF:
					IsPowered = false;
					return;

				case POWER_TOGGLE:
					IsPowered = !IsPowered;
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

			if (s_ScalingModeMap.ContainsValue(command))
			{
				ScalingMode = s_ScalingModeMap.GetKey(command);
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
			Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToHexLiteral(command));


			if (SerialQueue == null)
				return;

			// Re-queue power on or input select commands that fail
			if (command == POWER_ON)
			{
				m_PowerRetries++;
				if (m_PowerRetries > MAX_RETRIES)
				{
					Log(eSeverity.Error, "Power On Command for Samsung Display Reached Max Retries, aborting.");
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
					Log(eSeverity.Error, "Input Command for Samsung Display Reached Max Retries, aborting.");
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
				IsPowered = true;
				ActiveInput = s_InputMap.ContainsValue(command)
					            ? s_InputMap.GetKey(command)
					            : (int?)null;

				m_InputRetries = 0;

				return;
			}

			// Scaling Mode
			if (s_ScalingModeMap.Values.Contains(command))
			{
				IsPowered = true;
				ScalingMode = s_ScalingModeMap.GetKey(command);
				return;
			}

			// Volume
			if (command.StartsWith(VOLUME, StringComparison.Ordinal))
			{
				IsPowered = true;
				Volume = command[5];
				IsMuted = false;
				return;
			}

			switch (command)
			{
				case POWER_ON:
					IsPowered = true;
					m_PowerRetries = 0;
					return;
				case POWER_OFF:
					IsPowered = false;
					m_PowerRetries = 0;
					return;
				case MUTE_ON:
					IsPowered = true;
					IsMuted = true;
					return;
				case MUTE_OFF:
					IsPowered = true;
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

			Log(eSeverity.Error, "Command {0} failed.", command);
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

			if (commandA.StartsWith(ASPECT_PREFIX, StringComparison.Ordinal) && commandB.StartsWith(ASPECT_PREFIX, StringComparison.Ordinal))
				return true;

			return false;
		}

		#endregion
	}
}
