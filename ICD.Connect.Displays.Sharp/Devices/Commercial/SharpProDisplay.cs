using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Sharp.Devices.Commercial
{
	/// <summary>
	/// SharpProDisplay provides methods for interacting with a Sharp TV.
	/// </summary>
	public sealed class SharpProDisplay : AbstractDisplayWithAudio<SharpProDisplaySettings>
	{
		/// <summary>
		/// TCP drops connection every 3 minutes without a command.
		/// </summary>
		private const long KEEP_ALIVE_INTERVAL = 2 * 60 * 1000;

		public const string RETURN = "\x0D\x0A";

		//private const string OK = "OK" + RETURN;
		private const string ERROR = "ERR" + RETURN;

		private const string POWER = "POWR";
		private const string MUTE = "MUTE";
		private const string VOLUME = "VOLM";
		private const string INPUT = "INPS";
		private const string WIDE = "WIDE";
		private const string QUERY = "????";

		private const string POWER_OFF = POWER + "0000" + RETURN;
		private const string POWER_ON = POWER + "0001" + RETURN;
		private const string POWER_QUERY = POWER + QUERY + RETURN;

		private const string MUTE_OFF = MUTE + "0000" + RETURN;
		private const string MUTE_ON = MUTE + "0001" + RETURN;
		private const string MUTE_QUERY = MUTE + QUERY + RETURN;

		private const int INPUT_HDMI_1_NUMERIC = 9;
		private const int INPUT_HDMI_2_NUMERIC = 12;

		private const string INPUT_HDMI_1 = INPUT + "0009" + RETURN;
		private const string INPUT_HDMI_2 = INPUT + "0012" + RETURN;
		private const string INPUT_HDMI_QUERY = INPUT + QUERY + RETURN;

		private const string VOLUME_QUERY = VOLUME + QUERY + RETURN;

		private const string SCALING_MODE_16_X9 = WIDE + "0001" + RETURN; // Wide
		private const string SCALING_MODE_4_X3 = WIDE + "0004" + RETURN; // Normal
		private const string SCALING_MODE_NO_SCALE = WIDE + "0005" + RETURN; // Dot by dot
		private const string SCALING_MODE_ZOOM = WIDE + "0002" + RETURN; // Zoom 1
		private const string SCALING_MODE_QUERY = WIDE + QUERY + RETURN;

		private const ushort VOLUME_INCREMENT = 1;

		private const int MAX_RETRY_ATTEMPTS = 20;

		/// <summary>
		/// Maps the Sharp view mode to the command.
		/// </summary>
		private static readonly BiDictionary<int, string> s_ViewModeMap =
			new BiDictionary<int, string>
			{
				{2, SCALING_MODE_4_X3},
				{3, SCALING_MODE_ZOOM},
				{4, SCALING_MODE_16_X9},
				{8, SCALING_MODE_NO_SCALE}
			};

		/// <summary>
		/// Maps scaling mode to command.
		/// </summary>
		private static readonly BiDictionary<eScalingMode, string> s_ScalingModeMap =
			new BiDictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, SCALING_MODE_16_X9},
				{eScalingMode.Square4X3, SCALING_MODE_4_X3},
				{eScalingMode.NoScale, SCALING_MODE_NO_SCALE},
				{eScalingMode.Zoom, SCALING_MODE_ZOOM}
			};

		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2}
		};

		/// <summary>
		/// The display simply gives us a numeric value for the HDMI input, so we do a reverse
		/// lookup to figure out which physical input we are on.
		/// </summary>
		private static readonly BiDictionary<int, int> s_ResponseToInputMap = new BiDictionary<int, int>
		{
			{INPUT_HDMI_1_NUMERIC, 1},
			{INPUT_HDMI_2_NUMERIC, 2}
		};

		private readonly Dictionary<string, int> m_RetryCounts = new Dictionary<string, int>();
		private readonly SafeCriticalSection m_RetryLock = new SafeCriticalSection();
		private readonly SafeTimer m_KeepAliveTimer;

		#region Properties

		/// <summary>
		/// Override if the display volume minimum is not 0.
		/// </summary>
		public override float VolumeDeviceMin { get { return 0; } }

		/// <summary>
		/// Override if the display volume maximum is not 100.
		/// </summary>
		public override float VolumeDeviceMax { get { return 31; } }

		/// <summary>
		/// Gets/sets the ID of this tv.
		/// </summary>
		[PublicAPI]
		public byte WallId { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SharpProDisplay()
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

			ISerialBuffer buffer = new SharpProSerialBuffer();
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
			SendCommand(POWER_ON);
			SendCommand(POWER_QUERY);
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			SendCommand(POWER_OFF);
			SendCommand(POWER_QUERY);
		}

		public override void MuteOn()
		{
			SendCommand(MUTE_ON);
			SendCommand(MUTE_QUERY);
		}

		public override void MuteOff()
		{
			SendCommand(MUTE_OFF);
			SendCommand(MUTE_QUERY);
		}

		protected override void VolumeSetRawFinal(float raw)
		{
            if (!VolumeControlAvailable)
                return;
			string command = GetCommand(VOLUME, ((ushort)raw).ToString());

			SendCommand(command, CommandComparer);
			SendCommand(VOLUME_QUERY, CommandComparer);
			SendCommand(MUTE_QUERY, CommandComparer);
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
			if (commandA.Contains(QUERY) != commandB.Contains(QUERY))
				return false;

			// Compare the first 4 characters (e.g. VOLM) to see if it's the same command type.
			return commandA.Substring(0, 4) == commandB.Substring(0, 4);
		}

		public override void VolumeUpIncrement()
		{
            if (!VolumeControlAvailable)
                return;
			SetVolume((ushort)(Volume + VOLUME_INCREMENT));
		}

		public override void VolumeDownIncrement()
		{
            if (!VolumeControlAvailable)
                return;
			SetVolume((ushort)(Volume - VOLUME_INCREMENT));
		}

		public override void SetActiveInput(int address)
		{
			SendCommand(s_InputMap.GetValue(address));
			SendCommand(INPUT_HDMI_QUERY);
		}

		/// <summary>
		/// Builds the string for the command.
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		[PublicAPI]
		public static string GetCommand(string prefix, string parameters)
		{
			return prefix + parameters.PadLeft(4, '0') + RETURN;
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"/>
		public override void SetScalingMode(eScalingMode mode)
		{
			SendCommand(s_ScalingModeMap.GetValue(mode));
			SendCommand(SCALING_MODE_QUERY);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called periodically to maintain the connection with the display.
		/// </summary>
		private void KeepAliveCallback()
		{
			if (ConnectionStateManager.IsConnected && SerialQueue != null && SerialQueue.CommandCount == 0)
				SendCommand(POWER_QUERY);
		}

		private void SendCommand(string data)
		{
			SendCommand(new SerialData(data));
		}

		private void SendCommand(string data, Func<string, string, bool> comparer)
		{
			SendCommand(new SerialData(data), (a, b) => comparer(a.Serialize(), b.Serialize()));
		}

		/// <summary>
		/// Called when the display has powered on.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Update ourselves.
			SendCommand(POWER_QUERY);

			if (PowerState != ePowerState.PowerOn)
				return;

			SendCommand(INPUT_HDMI_QUERY);
			SendCommand(MUTE_QUERY);
			SendCommand(SCALING_MODE_QUERY);
			SendCommand(VOLUME_QUERY);
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

			if (args.Response == ERROR)
				ParseError(args);
			else if (args.Data.Serialize().Substring(4, 4) == QUERY)
				ParseQuery(args);
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
			string response = args.Response.Replace(RETURN, "");

			int responseValue;
			if (!StringUtils.TryParse(response, out responseValue))
				return;

			switch (args.Data.Serialize())
			{
				case POWER_QUERY:
					PowerState = responseValue == 1 ? ePowerState.PowerOn : ePowerState.PowerOff;
					break;

				case VOLUME_QUERY:
					Volume = (ushort)responseValue;
					break;

				case MUTE_QUERY:
					IsMuted = responseValue == 1;
					break;

				case INPUT_HDMI_QUERY:
					ActiveInput = s_ResponseToInputMap.ContainsKey(responseValue)
						            ? s_ResponseToInputMap.GetValue(responseValue)
						            : (int?)null;
					break;

				case SCALING_MODE_QUERY:
					if (s_ViewModeMap.ContainsKey(responseValue))
					{
						string command = s_ViewModeMap.GetValue(responseValue);
						ScalingMode = s_ScalingModeMap.GetKey(command);
					}
					else
						ScalingMode = eScalingMode.Unknown;
					break;
			}
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			RetryCommand(args.Data.Serialize());
		}

		private void RetryCommand(string command)
		{
			//Log(eSeverity.Error, "Command {0} failed.", StringUtils.ToMixedReadableHexLiteral(args.Data));
			IncrementRetryCount(command);
			if (GetRetryCount(command) <= MAX_RETRY_ATTEMPTS)
			{
				SerialQueue.EnqueuePriority(new SerialData(command));
			}
			else
			{
				Log(eSeverity.Error, "Command {0} hit the retry limit.", StringUtils.ToMixedReadableHexLiteral(command));
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
