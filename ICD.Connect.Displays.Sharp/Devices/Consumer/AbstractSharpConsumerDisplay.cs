using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Sharp.Devices.Consumer
{
	public abstract class AbstractSharpConsumerDisplay<TSettings> : AbstractDisplayWithAudio<TSettings>
		where TSettings : AbstractSharpConsumerDisplaySettings, new()
	{
		/// <summary>
		/// TCP drops connection every 3 minutes without a command.
		/// </summary>
		private const long KEEP_ALIVE_INTERVAL = 2 * 60 * 1000;

		private const int MAX_RETRY_ATTEMPTS = 500;

		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		// ReSharper disable once StaticFieldInGenericType
		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, SharpDisplayCommands.INPUT_HDMI_1},
			{2, SharpDisplayCommands.INPUT_HDMI_2},
			{3, SharpDisplayCommands.INPUT_HDMI_3},
			{4, SharpDisplayCommands.INPUT_HDMI_4}
		};

		private readonly Dictionary<string, int> m_RetryCounts = new Dictionary<string, int>();
		private readonly SafeCriticalSection m_RetryLock = new SafeCriticalSection();
		private readonly SafeTimer m_KeepAliveTimer;

		private int? m_RequestedInput;
		private bool? m_RequestedMute;
		private ePowerState? m_ExpectedPowerState;

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
		/// Constructor.
		/// </summary>
		protected AbstractSharpConsumerDisplay()
		{
			m_KeepAliveTimer = new SafeTimer(KeepAliveCallback, KEEP_ALIVE_INTERVAL, KEEP_ALIVE_INTERVAL);
		}

		/// <summary>
		/// Clears resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			m_KeepAliveTimer.Dispose();

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		public override void ConfigurePort(ISerialPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new MultiDelimiterSerialBuffer(SharpDisplayCommands.RETURN);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			m_ExpectedPowerState = ePowerState.PowerOn;

			SendCommandPriority(SharpDisplayCommands.POWER_ON, 0);
			SendCommandPriority(SharpDisplayCommands.POWER_QUERY, 0);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			// So we can PowerOn the TV later.
			PowerOnCommand();

			m_ExpectedPowerState = ePowerState.PowerOff;

			SendCommandPriority(SharpDisplayCommands.POWER_OFF, 1);
			SendCommandPriority(SharpDisplayCommands.POWER_QUERY, 1);
		}

		public override void MuteOn()
		{
			m_RequestedMute = true;
			SendCommand(SharpDisplayCommands.MUTE_QUERY);
		}

		public override void MuteOff()
		{
			m_RequestedMute = false;
			SendCommand(SharpDisplayCommands.MUTE_QUERY);
		}

		public override void MuteToggle()
		{
			SendCommand(SharpDisplayCommands.MUTE_TOGGLE);
			SendCommand(SharpDisplayCommands.MUTE_QUERY);
		}

		[PublicAPI]
		public void PowerOnCommand()
		{
			SendCommandPriority(SharpDisplayCommands.POWER_ON_COMMAND, 0);
		}

		protected override void SetVolumeFinal(float raw)
		{
            if (!VolumeControlAvailable)
                return;

			ushort volume = (ushort)Math.Round(raw);
			string command = SharpDisplayCommands.GetCommand(SharpDisplayCommands.VOLUME, volume.ToString());

			SendCommand(command, CommandComparer);
			SendCommand(SharpDisplayCommands.VOLUME_QUERY, CommandComparer);
			SendCommand(SharpDisplayCommands.MUTE_QUERY, CommandComparer);
		}

		public override void VolumeUpIncrement()
		{
            if (!VolumeControlAvailable)
                return;

			SendCommand(SharpDisplayCommands.VOLUME_UP, CommandComparer);
			SendCommand(SharpDisplayCommands.VOLUME_QUERY, CommandComparer);
			SendCommand(SharpDisplayCommands.MUTE_QUERY, CommandComparer);
		}

		public override void VolumeDownIncrement()
		{
            if (!VolumeControlAvailable)
                return;

			SendCommand(SharpDisplayCommands.VOLUME_DOWN, CommandComparer);
			SendCommand(SharpDisplayCommands.VOLUME_QUERY, CommandComparer);
			SendCommand(SharpDisplayCommands.MUTE_QUERY, CommandComparer);
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

		public override void SetActiveInput(int address)
		{
			m_RequestedInput = address;
			SendCommand(SharpDisplayCommands.INPUT_HDMI_QUERY);
		}

		public void SendCommand(string data)
		{
			SendCommand(new SerialData(data));
		}

		public void SendCommandPriority(string data, int priority)
		{
			SendCommandPriority(new SerialData(data), priority);
		}

		public void SendCommand(string data, Func<string, string, bool> comparer)
		{
			SendCommand(new SerialData(data), (a, b) => comparer(a.Serialize(), b.Serialize()));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called periodically to maintain the connection with the display.
		/// </summary>
		private void KeepAliveCallback()
		{
			if (ConnectionStateManager.IsConnected && SerialQueue != null && SerialQueue.CommandCount == 0)
				SendCommand(SharpDisplayCommands.POWER_QUERY);
		}

		/// <summary>
		/// Prevents multiple volume commands being queued.
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		private static bool CommandComparer(string commandA, string commandB)
		{
			// If one is a query and the other is not, the commands are different.
			if (commandA.Contains(SharpDisplayCommands.QUERY) != commandB.Contains(SharpDisplayCommands.QUERY))
				return false;

			// Compare the first 4 characters (e.g. VOLM) to see if it's the same command type.
			return commandA.Substring(0, 4) == commandB.Substring(0, 4);
		}

		private int GetRetryCount(string command)
		{
			m_RetryLock.Enter();

			try
			{
				return m_RetryCounts.ContainsKey(command) ? m_RetryCounts[command] : 0;
			}
			finally
			{
				m_RetryLock.Leave();
			}
		}

		/// <summary>
		/// Called when the display has powered on.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			SendCommand(SharpDisplayCommands.POWER_QUERY);
			SendCommand(SharpDisplayCommands.INPUT_HDMI_QUERY);
			SendCommand(SharpDisplayCommands.MUTE_QUERY);
			SendCommand(SharpDisplayCommands.VOLUME_QUERY);
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

			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			if (args.Data == null)
				return;

			string responseWithDelimiter = args.Response + SharpDisplayCommands.RETURN;

			switch (responseWithDelimiter)
			{
				case SharpDisplayCommands.ERROR:
					ParseError(args);
					break;

				default:
					if (args.Data.Serialize().Substring(4, 4) == SharpDisplayCommands.QUERY)
						ParseQuery(args);
					ResetRetryCount(args.Data.Serialize());
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
			RetryCommand(args.Data.Serialize());
		}

		/// <summary>
		/// Called when a query command is successful.
		/// </summary>
		/// <param name="args"></param>
		private void ParseQuery(SerialResponseEventArgs args)
		{
			string response = args.Response.Replace(SharpDisplayCommands.RETURN, "");

			int responseValue;
			if (!StringUtils.TryParse(response, out responseValue))
				return;

			switch (args.Data.Serialize())
			{
				case SharpDisplayCommands.POWER_QUERY:
					PowerState = responseValue == 1 ? ePowerState.PowerOn : ePowerState.PowerOff;
					if (m_ExpectedPowerState == PowerState)
						m_ExpectedPowerState = null;
					else if (m_ExpectedPowerState != null)
						SendCommand(SharpDisplayCommands.POWER_QUERY);

					break;

				case SharpDisplayCommands.VOLUME_QUERY:
					Volume = (ushort)responseValue;
					break;

				case SharpDisplayCommands.MUTE_QUERY:
					IsMuted = responseValue == 1;
					if (m_RequestedMute != null)
						if (IsMuted != m_RequestedMute)
						{
							SendCommand(m_RequestedMute.Value ? SharpDisplayCommands.MUTE_ON : SharpDisplayCommands.MUTE_OFF);
							SendCommand(SharpDisplayCommands.MUTE_QUERY);
						}
						else
							m_RequestedMute = null;
					break;

				case SharpDisplayCommands.INPUT_HDMI_QUERY:
					ActiveInput = responseValue;
					if (m_RequestedInput != null)
						if (responseValue != m_RequestedInput)
						{
							SendCommand(s_InputMap.GetValue((int)m_RequestedInput));
							SendCommand(SharpDisplayCommands.INPUT_HDMI_QUERY);
						}
						else
							m_RequestedInput = null;
					break;
			}
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			string command = args.Data.Serialize();
			string commandType = command.Substring(0, 4);
			bool query = command.Substring(4, 4) == SharpDisplayCommands.QUERY;
			
			// Check to see if we are getting an error because we are setting a value to the same thing
			if (!query)
			{
				switch (commandType)
				{
					case SharpDisplayCommands.INPUT:
						int input;
						if (StringUtils.TryParse(command.Substring(4, 4).Trim(), out input))
						{
							// If input set errored and we are still requesting an input,
							// Retry input query (so it will eventually hit retry limit
							if (m_RequestedInput != null && PowerState == ePowerState.PowerOn)
							{
								RetryCommand(SharpDisplayCommands.INPUT_HDMI_QUERY);
								return;
							}
							// If the input isn't being requested any more, or the display is powered off, ignore the error
							if (m_RequestedInput == null || PowerState != ePowerState.PowerOn)
								return;
						}
						break;
				}
			}

			RetryCommand(args.Data.Serialize());
		}

		private void RetryCommand(string command)
		{
			IncrementRetryCount(command);
			if (GetRetryCount(command) <= MAX_RETRY_ATTEMPTS)
				SerialQueue.EnqueuePriority(new SerialData(command));
			else
			{
				Log(eSeverity.Error, "Command {0} failed too many times and hit the retry limit.",
				    StringUtils.ToMixedReadableHexLiteral(command));
				ResetRetryCount(command);
			}
		}

		private void IncrementRetryCount(string command)
		{
			m_RetryLock.Enter();

			try
			{
				if (m_RetryCounts.ContainsKey(command))
					m_RetryCounts[command]++;
				else
					m_RetryCounts.Add(command, 1);
			}
			finally
			{
				m_RetryLock.Leave();
			}
		}

		private void ResetRetryCount(string command)
		{
			m_RetryLock.Enter();

			try
			{
				m_RetryCounts.Remove(command);
			}
			finally
			{
				m_RetryLock.Leave();
			}
		}

		#endregion
	}
}
