﻿using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Panasonic.Devices
{
	public sealed class PanasonicClassicDisplay : AbstractDisplayWithAudio<PanasonicClassicDisplaySettings>
	{
		#region Commands

		//When the first char of the actual command is A-E, the combined notation interprets it as a part of 
		// the hex notated first character, ie \x02AMT:0 becomes *MT:0
		// so these commands are entirely in hex

		private const char STX = '\x02';
		private const char ETX = '\x03';

		private const string FAILURE = "\x02\x45\x52\x34\x30\x31\x03";

		private const string POWER_ON = "\x02PON\x03";
		private const string POWER_OFF = "\x02POF\x03";
		private const string QUERY_POWER = "\x02QPW\x03";

		private const string MUTE_ON = "\x02\x41\x4d\x54\x3a\x31\x03";
		private const string MUTE_OFF = "\x02\x41\x4d\x54\x3a\x30\x03";
		private const string QUERY_MUTE = "\x02QAM\x03";

		private const string VOLUME_SET_TEMPLATE = "\x02\x41\x56\x4c\x3a{0}\x03";
		private const string QUERY_VOLUME = "\x02QAV\x03";

		private const string INPUT_TOGGLE = "\x02IMS\x03";
		private const string INPUT_SET_TEMPLATE = "\x02IMS:{0}\x03";
		private const string INPUT_HDMI1 = "HM1";
		private const string INPUT_HDMI2 = "HM2";
		private const string INPUT_DVI = "DV1";
		private const string INPUT_PC = "PC1";
		private const string INPUT_VIDEO = "VD1";
		private const string INPUT_USB = "UD1";
		private const string QUERY_INPUT = "\x02QMI\x03";

		#endregion

		private const int MAX_RETRY_ATTEMPTS = 500;
		private const int CONNECTION_WAIT_TIMEOUT_MS = 3 * 1000;
		private readonly Dictionary<string, int> m_RetryCounts = new Dictionary<string, int>();
		private readonly SafeCriticalSection m_RetryLock = new SafeCriticalSection();

		private bool m_IsIpControlled;
		private bool? m_ExpectedPowerState;
		private int m_TargetInput = 1;

		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
			{1, INPUT_HDMI1},
			{2, INPUT_HDMI2},
			{3, INPUT_DVI},
			{4, INPUT_PC},
			{5, INPUT_VIDEO},
			{6, INPUT_USB}
		};

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

			if (port is INetworkPort)
			{
				m_IsIpControlled = true;
				//Todo: Connection State Manager for IP
			}

			ISerialBuffer buffer = new BoundedSerialBuffer(STX, ETX);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port as ISerialPort);
			queue.SetBuffer(buffer);
			queue.Timeout = 10 * 1000;

			SetSerialQueue(queue);
		}

		protected override void QueryState()
		{
			base.QueryState();

			SendNonFormattedCommand(QUERY_POWER);

			if (PowerState != ePowerState.PowerOn)
				return;

			SendNonFormattedCommand(QUERY_INPUT);
			SendNonFormattedCommand(QUERY_VOLUME);
			SendNonFormattedCommand(QUERY_MUTE);
		}

		[PublicAPI]
		public override void PowerOn()
		{
			SendNonFormattedCommandPriority(POWER_ON, 0);
			m_ExpectedPowerState = true;
			QueryPower();
		}

		[PublicAPI]
		public override void PowerOff()
		{
			SendNonFormattedCommandPriority(POWER_OFF, 1);
			m_ExpectedPowerState = false;
			QueryPower();
		}

		[PublicAPI]
		public override void MuteOn()
		{
			if (!VolumeControlAvailable)
				return;
			SendNonFormattedCommand(MUTE_ON);
			SendNonFormattedCommand(QUERY_MUTE);
		}

		[PublicAPI]
		public override void MuteOff()
		{
			if (!VolumeControlAvailable)
				return;
			SendNonFormattedCommand(MUTE_OFF);
			SendNonFormattedCommand(QUERY_MUTE);
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

		[PublicAPI]
		public override void VolumeUpIncrement()
		{
			if (!VolumeControlAvailable)
				return;
			SendNonFormattedCommand(GenerateSetVolumeCommand((int)Volume + 1));
			SendNonFormattedCommand(QUERY_VOLUME);
		}

		[PublicAPI]
		public override void VolumeDownIncrement()
		{
			if (!VolumeControlAvailable)
				return;
			SendNonFormattedCommand(GenerateSetVolumeCommand((int)Volume - 1));
			SendNonFormattedCommand(QUERY_VOLUME);
		}

		[PublicAPI]
		protected override void SetVolumeFinal(float raw)
		{
			if (!VolumeControlAvailable)
				return;

			int volume = (int)Math.Round(raw);
			string setVolCommand = GenerateSetVolumeCommand(volume);
			SendNonFormattedCommand(setVolCommand);
			SendNonFormattedCommand(QUERY_VOLUME);
		}

		[PublicAPI]
		public override void SetActiveInput(int address)
		{
			if ((PowerState != ePowerState.PowerOn) && (m_ExpectedPowerState == null || !m_ExpectedPowerState.Value))
				return;
			SendNonFormattedCommand(string.Format(INPUT_SET_TEMPLATE, s_InputMap[address]));
			m_TargetInput = address;
		}

		[PublicAPI]
		public static string ExtractCommand(string data)
		{
			return data.Substring(1, 3);
		}

		[PublicAPI]
		public static string ExtractParameter(string data, int paramLength)
		{
			return data.Substring(5, paramLength);
		}

		#endregion

		#region Private Methods

		private void QueryPower()
		{
			var command = string.Format(QUERY_POWER);
			SendNonFormattedCommandPriority(command, 2);
		}

		/// <summary>
		/// Queues the data to be sent to the physical display.
		/// </summary>
		/// <param name="data"></param>
		private void SendNonFormattedCommand(string data)
		{
			SendNonFormattedCommand(data, (a, b) => a == b);
		}

		/// <summary>
		/// Queues the data to be sent to the physical display.
		/// Replaces an earlier command if found via the comparer.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="comparer"></param>
		private void SendNonFormattedCommand(string data, Func<string, string, bool> comparer)
		{
			SendCommand(new SerialData(data), (a, b) => comparer(a.Serialize(), b.Serialize()));
		}

		/// <summary>
		/// Queues the data to be sent to the physical display at the given priority.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="priority"></param>
		private void SendNonFormattedCommandPriority(string data, int priority)
		{
			SendCommandPriority(new SerialData(data), priority);
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
			if (args.Response == FAILURE)
				ParseError(args);
			else
				ParseSuccess(args);
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
			Logger.Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToHexLiteral(args.Data.Serialize()));
			RetryCommand(args.Data.Serialize());
		}

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="args"></param>
		private void ParseSuccess(SerialResponseEventArgs args)
		{
			string response = args.Response;
			string command = ExtractCommand(response);

			switch (command)
			{
				case "QPW":
					string powered = ExtractParameter(response, 1);
					PowerState = powered == "1" ? ePowerState.PowerOn : ePowerState.PowerOff;
					break;

				case "QAM":
					string muted = ExtractParameter(response, 1);
					IsMuted = muted == "1";
					break;

				case "QAV":
					string volume = ExtractParameter(response, 3);
					Volume = int.Parse(volume);
					IsMuted = false;
					break;

				case "QMI":
					string inputParam = ExtractParameter(response, 3);

					int input;
					bool valid = s_InputMap.TryGetKey(inputParam, out input);

					if (valid)
						ActiveInput = input;
					else
						ActiveInput = null;

					break;

				case "IMS":
					if ((PowerState != ePowerState.PowerOn)&&
						m_ExpectedPowerState != null &&
						m_ExpectedPowerState.Value)
					{
						PowerState = ePowerState.PowerOn;
						m_ExpectedPowerState = null;
					}
					else if ((PowerState == ePowerState.PowerOn)&&
					         m_ExpectedPowerState != null &&
					         !m_ExpectedPowerState.Value)
					{
						if (args.Data != null)
							RetryCommand(args.Data.Serialize());
					}
					else
					{
						ActiveInput = m_TargetInput;
					}
					break;
			}

			if (args.Data != null)
				ResetRetryCount(args.Data.Serialize());
		}

		private static string GenerateSetVolumeCommand(int volumePercent)
		{
			volumePercent = MathUtils.Clamp(volumePercent, 0, 100);
			// protocol expects volume percent to always be three characters
			string volumeString = volumePercent.ToString("D3");
			return string.Format(VOLUME_SET_TEMPLATE, volumeString);
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			if ((PowerState == ePowerState.PowerOn)
				&& ExtractCommand(args.Data.Serialize()) == "IMS"
				&& m_ExpectedPowerState != null
				&& !m_ExpectedPowerState.Value)
			{
				PowerState = ePowerState.PowerOff;
				m_ExpectedPowerState = null;
				ResetRetryCount(args.Data.Serialize());
				return;
			}

			RetryCommand(args.Data.Serialize());
		}

		private void RetryCommand(string command)
		{
			Logger.Log(eSeverity.Debug, "Retry {0}, {1} times", command, GetRetryCount(command));
			IncrementRetryCount(command);
			if (GetRetryCount(command) <= MAX_RETRY_ATTEMPTS)
				SendCommandPriority(new SerialData(command), 0);
			else
			{
				Logger.Log(eSeverity.Error, "Command {0} failed too many times and hit the retry limit.",
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

		#endregion
	}
}
