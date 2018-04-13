using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Sharp
{
	/// <summary>
	///     SharpDisplay provides methods for interacting with a Sharp TV.
	/// </summary>
	public sealed class SharpDisplay : AbstractDisplayWithAudio<SharpDisplaySettings>
	{
		public const string RETURN = "\x0D";

		//private const string OK = "OK" + RETURN;
		public const string ERROR = "ERR" + RETURN;

		public const string POWER = "POWR";
		public const string MUTE = "MUTE";
		public const string VOLUME = "VOLM";
		public const string INPUT = "IAVD";
		public const string WIDE = "WIDE";
		public const string REMOTE_CONTROL_BUTTONS = "RCKY";
		public const string QUERY = "????";

		public const string POWER_ON = POWER + "1   " + RETURN;
		public const string POWER_OFF = POWER + "0   " + RETURN;
		public const string POWER_ON_COMMAND = "RSPW1   " + RETURN;
		public const string POWER_QUERY = POWER + QUERY + RETURN;

		public const string MUTE_TOGGLE = MUTE + "0   " + RETURN;
		public const string MUTE_ON = MUTE + "1   " + RETURN;
		public const string MUTE_OFF = MUTE + "2   " + RETURN;
		public const string MUTE_QUERY = MUTE + QUERY + RETURN;

		public const string INPUT_HDMI_1 = INPUT + "1   " + RETURN;
		public const string INPUT_HDMI_2 = INPUT + "2   " + RETURN;
		public const string INPUT_HDMI_3 = INPUT + "3   " + RETURN;
		public const string INPUT_HDMI_4 = INPUT + "4   " + RETURN;
		public const string INPUT_HDMI_QUERY = INPUT + QUERY + RETURN;

		public const string VOLUME_DOWN = REMOTE_CONTROL_BUTTONS + "32  " + RETURN;
		public const string VOLUME_UP = REMOTE_CONTROL_BUTTONS + "33  " + RETURN;
		public const string VOLUME_QUERY = VOLUME + QUERY + RETURN;

		public const string SCALING_MODE_16_X9 = WIDE + "40  " + RETURN; // Stretch
		public const string SCALING_MODE_4_X3 = WIDE + "20  " + RETURN; // S. Stretch
		public const string SCALING_MODE_NO_SCALE = WIDE + "80  " + RETURN; // Dot by dot
		public const string SCALING_MODE_ZOOM = WIDE + "30  " + RETURN; // Zoom AV
		public const string SCALING_MODE_QUERY = WIDE + QUERY + RETURN;
		private const int MAX_RETRY_ATTEMPTS = 20;

		private bool m_WarmingUp;

		private SafeTimer m_WarmupRepeatPowerQueryTimer;

		private const long TIMER_MS = 3 * 1000;

		private bool WarmingUp
		{
			get
			{
				return m_WarmingUp;
			}
			set
			{
				m_WarmingUp = value;
				if (value)
				{
					m_WarmupRepeatPowerQueryTimer.Reset(TIMER_MS);
				}
				else
				{
					m_WarmupRepeatPowerQueryTimer.Stop();
				}
			}
		}

		/// <summary>
		///     Maps the Sharp view mode to the command.
		/// </summary>
		private static readonly Dictionary<int, string> s_ViewModeMap =
			new Dictionary<int, string>
			{
				{2, SCALING_MODE_4_X3},
				{3, SCALING_MODE_ZOOM},
				{4, SCALING_MODE_16_X9},
				{8, SCALING_MODE_NO_SCALE}
			};

		/// <summary>
		///     Maps scaling mode to command.
		/// </summary>
		private static readonly Dictionary<eScalingMode, string> s_ScalingModeMap =
			new Dictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, SCALING_MODE_16_X9},
				{eScalingMode.Square4X3, SCALING_MODE_4_X3},
				{eScalingMode.NoScale, SCALING_MODE_NO_SCALE},
				{eScalingMode.Zoom, SCALING_MODE_ZOOM}
			};

		/// <summary>
		///     Maps index to an input command.
		/// </summary>
		private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2},
			{3, INPUT_HDMI_3},
			{4, INPUT_HDMI_4}
		};

		#region Methods

		/// <summary>
		///     Sets and configures the port for communication with the physical display.
		/// </summary>
		public void SetPort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);

			ISerialBuffer buffer = new DelimiterSerialBuffer((char)Encoding.ASCII.GetBytes(RETURN)[0]);
			SerialQueue queue = new SerialQueue();
			queue.SetPort(port);
			queue.SetBuffer(buffer);
			queue.Timeout = 3 * 1000;

			SetSerialQueue(queue);
		}

		/// <summary>
		///     Configures a com port for communication with the physical display.
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
			SendCommand(POWER_ON);
			WarmingUp = true;
			SendCommand(POWER_QUERY);
		}

		public override void PowerOff()
		{
			// So we can PowerOn the TV later.
			PowerOnCommand();

			SendCommand(POWER_OFF);
			WarmingUp = false;
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

		public override void MuteToggle()
		{
			SendCommand(MUTE_TOGGLE);
			SendCommand(MUTE_QUERY);
		}

		[PublicAPI]
		public void PowerOnCommand()
		{
			SendCommand(POWER_ON_COMMAND);
		}

		protected override void VolumeSetRawFinal(float raw)
		{
            if (!IsPowered)
                return;
			string command = GetCommand(VOLUME, raw.ToString());

			SendCommand(command, CommandComparer);
			SendCommand(VOLUME_QUERY, CommandComparer);
			SendCommand(MUTE_QUERY, CommandComparer);
		}

		/// <summary>
		///     Prevents multiple volume commands being queued.
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
            if (!IsPowered)
                return;
			SendCommand(VOLUME_UP, CommandComparer);
			SendCommand(VOLUME_QUERY, CommandComparer);
			SendCommand(MUTE_QUERY, CommandComparer);
		}

		public override void VolumeDownIncrement()
		{
            if (!IsPowered)
                return;
			SendCommand(VOLUME_DOWN, CommandComparer);
			SendCommand(VOLUME_QUERY, CommandComparer);
			SendCommand(MUTE_QUERY, CommandComparer);
		}

		public override void SetHdmiInput(int address)
		{
			SendCommand(s_InputMap[address]);
			SendCommand(INPUT_HDMI_QUERY);
		}

		/// <summary>
		///     Builds the string for the command.
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		[PublicAPI]
		public static string GetCommand(string prefix, string parameters)
		{
			return prefix + parameters.PadRight(4, ' ') + RETURN;
		}

		/// <summary>
		///     Sets the scaling mode.
		/// </summary>
		/// <param name="mode" />
		public override void SetScalingMode(eScalingMode mode)
		{
			SendCommand(s_ScalingModeMap[mode]);
			SendCommand(SCALING_MODE_QUERY);
		}

		#endregion

		#region Settings

		/// <summary>
		///     Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SharpDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			if (SerialQueue != null && SerialQueue.Port != null)
				settings.Port = SerialQueue.Port.Id;
			else
				settings.Port = null;
		}

		/// <summary>
		///     Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetPort(null);
		}

		/// <summary>
		///     Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SharpDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Logger.AddEntry(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
			}

			SetPort(port);

			m_WarmupRepeatPowerQueryTimer = SafeTimer.Stopped(WarmupRepeatPowerQueryTimerOnElapsed);
		}

		#endregion

		#region Private Methods

		private void WarmupRepeatPowerQueryTimerOnElapsed()
		{
			SendCommand(POWER_QUERY);
			SendCommand(INPUT_HDMI_QUERY);
			if(m_WarmingUp)
				m_WarmupRepeatPowerQueryTimer.Reset(TIMER_MS);
		}


		public void SendCommand(string data)
		{
			SendCommand(new SerialData(data));
		}

		public void SendCommand(string data, Func<string, string, bool> comparer)
		{
			SendCommand(new SerialData(data), (a, b) => comparer(a.Serialize(), b.Serialize()));
		}

		/// <summary>
		///     Called when the display has powered on.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Update ourselves.
			SendCommand(POWER_QUERY);

			if (!IsPowered)
				return;

			SendCommand(INPUT_HDMI_QUERY);
			SendCommand(MUTE_QUERY);
			SendCommand(SCALING_MODE_QUERY);
			SendCommand(VOLUME_QUERY);
		}

		/// <summary>
		///     Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			if (args.Data == null)
				return;

			if (args.Response == ERROR)
				ParseError(args);
			else
			{
				if (args.Data.Serialize().Substring(4, 4) == QUERY)
					ParseQuery(args);

				ResetRetryCount(args.Data.Serialize());
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
		///     Called when a query command is successful.
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
					IsPowered = responseValue == 1;
					break;

				case VOLUME_QUERY:
					Volume = (ushort)responseValue;
					break;

				case MUTE_QUERY:
					IsMuted = responseValue == 1;
					break;

				case INPUT_HDMI_QUERY:
					HdmiInput = responseValue;
					if (IsPowered)
						WarmingUp = false;
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
		}

		/// <summary>
		///     Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			RetryCommand(args.Data.Serialize());
		}

		private void RetryCommand(string command)
		{
			//Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToMixedReadableHexLiteral(args.Data));
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

		private readonly Dictionary<string, int> m_RetryCounts = new Dictionary<string, int>();
		private readonly SafeCriticalSection m_RetryLock = new SafeCriticalSection();

		/// <summary>
		///     Gets the number of HDMI inputs.
		/// </summary>
		public override int InputCount { get { return s_InputMap.Count; } }
	}
}
