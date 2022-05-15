using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using System;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.Settings;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	public abstract class AbstractSamsungDisplay<T> : AbstractDisplayWithAudio<T> where T : ISamsungDisplaySettings, new()
	{
		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, SamsungCommand.INPUT_HDMI_1},
			{2, SamsungCommand.INPUT_HDMI_2},
			{3, SamsungCommand.INPUT_HDMI_3},
			{4, SamsungCommand.INPUT_HDMI_4},
			{11, SamsungCommand.INPUT_AV_1},
			{12, SamsungCommand.INPUT_AV_2},
			{13, SamsungCommand.INPUT_AV_3},
			{21, SamsungCommand.INPUT_SVIDEO_1},
			{22, SamsungCommand.INPUT_SVIDEO_2},
			{23, SamsungCommand.INPUT_SVIDEO_3},
			{31, SamsungCommand.INPUT_COMPONENT_1},
			{32, SamsungCommand.INPUT_COMPONENT_2},
			{33, SamsungCommand.INPUT_COMPONENT_3},
			{40, SamsungCommand.INPUT_TV}
		};

		protected int PowerRetries { get; set; }
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
			SendNonFormattedCommand(SamsungCommand.POWER_ON, CommandComparer, SamsungCommand.PRIORITY_POWER_INITIAL);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		[PublicAPI]
		public override void PowerOff()
		{
			if (SerialQueue == null)
				return;
			Logger.Log(eSeverity.Debug, "Display Power Off while {0} commands were enqueued. Commands dropped.", SerialQueue.CommandCount);
			SerialQueue.Clear();

			SendNonFormattedCommand(SamsungCommand.POWER_OFF, CommandComparer, SamsungCommand.PRIORITY_POWER_INITIAL);
		}

		[PublicAPI]
		public void PowerToggle()
		{
			SendNonFormattedCommand(SamsungCommand.POWER_TOGGLE, CommandComparer, SamsungCommand.PRIORITY_POWER_INITIAL);
		}

		public override void MuteOn()
		{
			SendNonFormattedCommand(SamsungCommand.MUTE_ON, CommandComparer);
		}

		public override void MuteOff()
		{
			SendNonFormattedCommand(SamsungCommand.MUTE_OFF, CommandComparer);
		}

		public override void MuteToggle()
		{
			SendNonFormattedCommand(SamsungCommand.MUTE_TOGGLE, CommandComparer);
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
			SendNonFormattedCommand(SamsungCommand.VOLUME + (char)volume, VolumeComparer);
		}

		/// <summary>
		/// Prevents multiple volume commands from being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool VolumeComparer(string commandA, string commandB)
		{
			return commandA.StartsWith(SamsungCommand.VOLUME, StringComparison.Ordinal) && commandB.StartsWith(SamsungCommand.VOLUME, StringComparison.Ordinal);
		}

		public override void VolumeUpIncrement()
		{
			if (!VolumeControlAvailable)
				return;

			SendNonFormattedCommand(SamsungCommand.VOLUME_UP);
		}

		public override void VolumeDownIncrement()
		{
			if (!VolumeControlAvailable)
				return;

			SendNonFormattedCommand(SamsungCommand.VOLUME_DOWN);
		}

		public override void SetActiveInput(int address)
		{
			SendNonFormattedCommand(s_InputMap.GetValue(address), CommandComparer, SamsungCommand.PRIORITY_INPUT_INITIAL);
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
			SendNonFormattedCommand(data, SamsungCommand.PRIORITY_DEFAULT);
		}

		/// <summary>
		/// Calculates the checksum and queues the data to be sent to the physical display.
		/// Replaces an earlier command if found via the comparer.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="comparer"></param>
		private void SendNonFormattedCommand(string data, Func<string, string, bool> comparer)
		{
			SendNonFormattedCommand(data, comparer, SamsungCommand.PRIORITY_DEFAULT);
		}

		/// <summary>
		/// Calculates the checksum and queues the data to be sent to the physical display.
		/// Replaces an earlier command if found via the comparer.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="comparer"></param>
		/// <param name="priority"></param>
		protected void SendNonFormattedCommand(string data, Func<string, string, bool> comparer, int priority)
		{
			data += GetCheckSum(data);

			SendCommandPriority(new SerialData(data), (a, b) => comparer(a.Serialize(), b.Serialize()), priority);
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
				case SamsungCommand.POWER_ON:
					PowerState = ePowerState.PowerOn;
					return;

				case SamsungCommand.POWER_OFF:
					PowerState = ePowerState.PowerOff;
					return;

				case SamsungCommand.POWER_TOGGLE:
					PowerState = PowerState == ePowerState.PowerOn ? ePowerState.PowerOff : ePowerState.PowerOn;
					return;

				case SamsungCommand.MUTE_ON:
					IsMuted = true;
					return;

				case SamsungCommand.MUTE_OFF:
					IsMuted = false;
					return;

				case SamsungCommand.MUTE_TOGGLE:
					IsMuted = !IsMuted;
					return;
			}

			if (command.StartsWith(SamsungCommand.VOLUME, StringComparison.Ordinal))
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
			if (args.Response.EndsWith(SamsungCommand.FAILURE))
				ParseError(args);
			else if (args.Response.EndsWith(SamsungCommand.SUCCESS))
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
			if (command == SamsungCommand.POWER_ON)
			{
				PowerRetries++;
				if (PowerRetries > SamsungCommand.MAX_RETRIES)
				{
					Logger.Log(eSeverity.Error, "Power On Command for Samsung Display Reached Max Retries, aborting.");
					PowerRetries = 0;
					return;
				}
				SerialQueue.EnqueuePriority(new SerialData(args.Data.Serialize()), SamsungCommand.PRIORITY_POWER_RETRY);
			}
			else if (s_InputMap.ContainsValue(command))
			{
				m_InputRetries++;

				// If input commands hit a specified limit, enqueue a power on command at higher priority to make sure the display is actually powered on)
				if (m_InputRetries > SamsungCommand.MAX_RETRIES / 2)
				{
					SendNonFormattedCommand(SamsungCommand.POWER_ON, CommandComparer, SamsungCommand.PRIORITY_POWER_RETRY);
				}

				if (m_InputRetries > SamsungCommand.MAX_RETRIES)
				{
					Logger.Log(eSeverity.Error, "Input Command for Samsung Display Reached Max Retries, aborting.");
					m_InputRetries = 0;
					return;
				}
				SerialQueue.EnqueuePriority(args.Data, SamsungCommand.PRIORITY_INPUT_RETRY);
			}
		}

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="args"></param>
		protected virtual void ParseSuccess(SerialResponseEventArgs args)
		{
			if (args.Data == null)
				return;

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
			if (command.StartsWith(SamsungCommand.VOLUME, StringComparison.Ordinal))
			{
				PowerState = ePowerState.PowerOn;
				Volume = command[5];
				IsMuted = false;
				return;
			}

			switch (command)
			{
				case SamsungCommand.POWER_ON:
					PowerState = ePowerState.PowerOn;
					PowerRetries = 0;
					return;
				case SamsungCommand.POWER_OFF:
					PowerState = ePowerState.PowerOff;
					PowerRetries = 0;
					return;
				case SamsungCommand.MUTE_ON:
					PowerState = ePowerState.PowerOn;
					IsMuted = true;
					return;
				case SamsungCommand.MUTE_OFF:
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
		protected static bool CommandComparer(string commandA, string commandB)
		{
			if (commandA.StartsWith(SamsungCommand.POWER_PREFIX, StringComparison.Ordinal) && commandB.StartsWith(SamsungCommand.POWER_PREFIX, StringComparison.Ordinal))
				return true;

			if (commandA.StartsWith(SamsungCommand.MUTE_PREFIX, StringComparison.Ordinal) && commandB.StartsWith(SamsungCommand.MUTE_PREFIX, StringComparison.Ordinal))
				return true;

			if (commandA.StartsWith(SamsungCommand.INPUT_PREFIX, StringComparison.Ordinal) && commandB.StartsWith(SamsungCommand.INPUT_PREFIX, StringComparison.Ordinal))
				return true;

			return false;
		}

		#endregion
	}
}