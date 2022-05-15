using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.Samsung.Controls;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public abstract class AbstractSamsungProDisplay<T> : AbstractDisplayWithAudio<T>, ISamsungProDisplay
		where T : ISamsungProDisplaySettings, new()
	{
		private const int MAX_RETRIES = 40;
		private const long DISCONNECT_CLEAR_TIME = 10 * 1000;

		private const byte POWER = 0x11;
		private const byte VOLUME = 0x12;
		private const byte MUTE = 0x13;
		private const byte INPUT = 0x14;
		private const byte LAUNCHER = 0xC7;

		private const byte INPUT_HDMI_1 = 0x21;
		private const byte INPUT_HDMI_1_PC = 0x22;
		private const byte INPUT_HDMI_2 = 0x23;
		private const byte INPUT_HDMI_2_PC = 0x24;
		private const byte INPUT_HDMI_3 = 0x31;
		private const byte INPUT_HDMI_3_PC = 0x31;
		private const byte INPUT_DISPLAYPORT = 0x25;
		private const byte INPUT_DVI = 0x18;
		private const byte INPUT_DVI_VIDEO = 0x1F;
		private const byte INPUT_URL_LAUNCHER = 0x63;

		private const ushort VOLUME_INCREMENT = 1;

		private const byte LAUNCHER_MODE = 0x81;
		private const byte LAUNCHER_MODE_MAGIC_INFO = 0x00;
		private const byte LAUNCHER_MODE_URL_LAUNCHER = 0x01;
		private const byte LAUNCHER_MODE_MAGIC_IWB = 0x02;

		private const byte LAUNCHER_ADDRESS = 0x82;

		// ReSharper disable StaticFieldInGenericType
		/// <summary>
		/// Maps index to an input command.
		/// </summary>
		private static readonly BiDictionary<int, byte> s_InputMap = new BiDictionary<int, byte>
		{
			{1, INPUT_HDMI_1},
			{2, INPUT_HDMI_2},
			{3, INPUT_HDMI_3},
			{4, INPUT_DISPLAYPORT},
			{5, INPUT_DVI},
			{SamsungProDisplayDestinationControl.URL_LAUNCHER_INPUT, INPUT_URL_LAUNCHER }
		};

		private static readonly BiDictionary<int, byte> s_InputPcMap = new BiDictionary<int, byte>
		{
			{1, INPUT_HDMI_1_PC},
			{2, INPUT_HDMI_2_PC},
			{3, INPUT_HDMI_3_PC},
			{5, INPUT_DVI_VIDEO}
		};

		private static readonly Dictionary<eLauncherMode, byte[]> s_LauncherModeMap = new Dictionary<eLauncherMode, byte[]>
		{
			{eLauncherMode.Unknown, new byte[0] },
			{eLauncherMode.MagicInfo, new[] { LAUNCHER_MODE_MAGIC_INFO } },
			{eLauncherMode.Url, new[] { LAUNCHER_MODE_URL_LAUNCHER } },
			{eLauncherMode.MagicIwb, new[] { LAUNCHER_MODE_MAGIC_IWB } }
		};
		// ReSharper restore StaticFieldInGenericType

		private readonly Dictionary<ISamsungProCommand, int> m_CommandRetries;

		private string m_AbsoluteUri;
		private Uri m_LauncherUri;

		private enum eLauncherMode
		{
			Unknown = 0,
			MagicInfo = 1,
			Url = 2,
			MagicIwb = 3
		}

		#region Events

		public event EventHandler<GenericEventArgs<Uri>> OnUrlLauncherSourceChanged;

		#endregion

		#region Properties

		private bool DisableLauncher { get; set; }

		private eLauncherMode LauncherMode { get; set; }

		private string LastRequestedUri { get; set; }

		[CanBeNull]
		private string AbsoluteUri
		{
			get { return m_AbsoluteUri; }
			set
			{
				if (m_AbsoluteUri == value)
					return;

				m_AbsoluteUri = value;

				try
				{
					LauncherUri = m_AbsoluteUri == null ? null : new Uri(m_AbsoluteUri);
				}
				catch (Exception)
				{
				}
			}
		}

		public Uri LauncherUri
		{
			get { return m_LauncherUri; }
			private set
			{
				if (value == m_LauncherUri)
					return;

				m_LauncherUri = value;
				OnUrlLauncherSourceChanged.Raise(this, LauncherUri);
			}
		}

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

		#endregion

		protected AbstractSamsungProDisplay()
		{
			m_CommandRetries = new Dictionary<ISamsungProCommand, int>(new SamsungProCommandEqualityComparer());
		}

		#region Methods

		protected abstract byte GetWallIdForPowerCommand();

		protected abstract byte GetWallIdForInputCommand();

		protected abstract byte GetWallIdForVolumeCommand();

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		public override void ConfigurePort(IPort port)
		{
			base.ConfigurePort(port);

			ISerialBuffer buffer = new SamsungProDisplayBuffer();
			SerialQueue queue = new SerialQueue
			{
				DisconnectClearTime = DISCONNECT_CLEAR_TIME
			};
			queue.SetPort(port as ISerialPort);
			queue.SetBuffer(buffer);
			queue.Timeout = 3 * 1000;

			SetSerialQueue(queue);

			ISerialPort serialPort = port as ISerialPort;
			if (serialPort != null && serialPort.IsConnected)
				QueryState();
		}

		public override void PowerOn()
		{
			SendCommand(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 1));
		}

		public override void PowerOff()
		{
			SendCommand(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 0));
		}

		public override void SetActiveInput(int address)
		{
			SendCommand(new SamsungProCommand(INPUT, GetWallIdForInputCommand(), s_InputMap.GetValue(address)));
		}

		public override void MuteOn()
		{
			SendCommand(new SamsungProCommand(MUTE, GetWallIdForVolumeCommand(), 1));
		}

		public override void MuteOff()
		{
			SendCommand(new SamsungProCommand(MUTE, GetWallIdForVolumeCommand(), 0));
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
			byte volume = (byte)Math.Round(raw);

			SendCommand(new SamsungProCommand(VOLUME, GetWallIdForVolumeCommand(), volume), CommandComparer);

			// Display unmutes on volume change, if and only if its currently muted
			if (IsMuted)
				SendCommand(new SamsungProCommand(MUTE, GetWallIdForVolumeCommand(), 0).ToQuery(), CommandComparer);
		}

		/// <summary>
		/// Prevents multiple volume commands from being queued.
		/// </summary>
		/// <param name="dataA"></param>
		/// <param name="dataB"></param>
		/// <returns></returns>
		private static bool CommandComparer(ISerialData dataA, ISerialData dataB)
		{
			AbstractSamsungProCommand commandA = (AbstractSamsungProCommand)dataA;
			AbstractSamsungProCommand commandB = (AbstractSamsungProCommand)dataB;

			// If one is a query and the other is not, the commands are different.
			if (commandA.GetType() != commandB.GetType())
				return false;

			// Are the command types the same?
			return commandA.Command == commandB.Command;
		}

		public override void VolumeUpIncrement()
		{
			if (PowerState != ePowerState.PowerOn)
				return;

			SetVolume((ushort)(Volume + VOLUME_INCREMENT));
		}

		public override void VolumeDownIncrement()
		{
			if (PowerState != ePowerState.PowerOn)
				return;

			SetVolume((ushort)(Volume - VOLUME_INCREMENT));
		}

		public void SetUrlLauncherSource(Uri source)
		{
			if (DisableLauncher)
				throw new InvalidOperationException("Launcher is Disabled");

			// Cache the requested source in case of command failure.
			LastRequestedUri = source == null ? null : source.AbsoluteUri;

			if (PowerState != ePowerState.PowerOn)
				return;

			// If the source is null clear the configured URL on the display.
			if (source == null)
			{
				SendCommand(new SamsungProCommand(LAUNCHER, GetWallIdForInputCommand(), LAUNCHER_ADDRESS,
				                                     new byte[] {0x00}));
				return;
			}

			if (LauncherMode != eLauncherMode.Url)
			{
				Logger.Log(eSeverity.Warning, "Attempting to set launcher url while display is in mode {0} ... Changing to url launcher mode", LauncherMode);
				SetLauncherMode(eLauncherMode.Url);
			}

			byte[] urlBytes = Encoding.ASCII.GetBytes(source.AbsoluteUri);
			SendCommand(new SamsungProCommand(LAUNCHER, GetWallIdForInputCommand(), LAUNCHER_ADDRESS, urlBytes));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Allows for displays to specify their own custom destination control or just use the default display one.
		/// </summary>
		/// <param name="addControl"></param>
		protected override void AddDisplayDestinationControl(Action<IDeviceControl> addControl)
		{
			addControl(new SamsungProDisplayDestinationControl(this, 0));
		}

		protected override void RaisePowerStateChanged(ePowerState state)
		{
			base.RaisePowerStateChanged(state);

			// When we power on check to make sure the last requested uri actually made it to the display.
			// If it didn't make it set the url again.
			if (state == ePowerState.PowerOn && LastRequestedUri != null && LastRequestedUri != AbsoluteUri)
			{
				try
				{
					SetUrlLauncherSource(new Uri(LastRequestedUri));
				}
				catch (Exception e)
				{
					Logger.Log(eSeverity.Error, "Error updating URL launcher source for display - {0}", e.Message);
				}
			}
		}

		/// <summary>
		/// Polls the physical device for the current state.
		/// </summary>
		protected override void QueryState()
		{
			base.QueryState();

			// Important - Some SamsungPro models get really upset if you try to send commands
			// while it's warming up. Sending the queries first by priority seems to solve this problem.

			// Query the state of the device
			SendCommandPriority(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 0).ToQuery(), int.MinValue);

			if (PowerState != ePowerState.PowerOn)
				return;

			SendCommandPriority(new SamsungProCommand(VOLUME, GetWallIdForVolumeCommand(), 0).ToQuery(), int.MinValue);
			SendCommandPriority(new SamsungProCommand(INPUT, GetWallIdForInputCommand(), 0).ToQuery(), int.MinValue);
			SendCommandPriority(new SamsungProCommand(MUTE, GetWallIdForVolumeCommand(), 0).ToQuery(), int.MinValue);
			if (!DisableLauncher)
			{
				SendCommandPriority(
				                    new SamsungProCommand(LAUNCHER, GetWallIdForInputCommand(), LAUNCHER_MODE, new byte[0]).ToQuery(),
				                    int.MinValue);
				SendCommandPriority(
				                    new SamsungProCommand(LAUNCHER, GetWallIdForInputCommand(), LAUNCHER_ADDRESS, new byte[0])
					                    .ToQuery(), int.MinValue);
			}
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

			SamsungProCommand command = args.Data as SamsungProCommand;
			if (command == null)
				return;

			switch (command.Command)
			{
				case POWER:
					PowerState = command.Data[0] == 1 ? ePowerState.PowerOn : ePowerState.PowerOff;
					return;

				case VOLUME:
					Volume = command.Data[0];
					return;

				case MUTE:
					IsMuted = command.Data[0] == 1;
					return;

				case INPUT:
					ActiveInput = s_InputMap.GetKey(command.Data[0]);
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
			SamsungProResponse response = new SamsungProResponse(args.Response);

			switch (response.Header)
			{
				case 0xAA:
					switch (response.Code)
					{
						// Normal response
						case 0xFF:
							if (!response.IsValid)
								return;

							if (response.Success)
								ParseSuccess(args);
							else
								ParseError(args);
							break;

						// Cooldown
						case 0xE1:
							// Ignore unsolicited cooldown message
							return;
					}
					break;

				case 0xFF:
				case 0x1C:
					switch (response.Code)
					{
						// Warmup
						case 0x1C:
						case 0xC4:
							// Keep sending power query until fully powered on
							SendCommandPriority(new SamsungProCommand(POWER, GetWallIdForPowerCommand(), 0).ToQuery(), 0);
							break;
					}
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
			ISamsungProCommand command = args.Data as ISamsungProCommand;
			
			if (command != null)
				RetryCommand(command);
			else
				Logger.Log(eSeverity.Error, "Command {0} timed out, unable to retry.", StringUtils.ToHexLiteral(args.Data.Serialize()));
		}

		protected override void SerialQueueOnSendFailed(object sender, SerialDataEventArgs args)
		{
			ISamsungProCommand command = args.Data as ISamsungProCommand;

			if (command != null)
				RetryCommand(command);
			else
				Logger.Log(eSeverity.Error, "Command {0} failed to send, unable to retry.", StringUtils.ToHexLiteral(args.Data.Serialize()));
		}

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="args"></param>
		protected virtual void ParseSuccess(SerialResponseEventArgs args)
		{
			// Clear retry counter
			ISamsungProCommand command = args.Data as ISamsungProCommand;
			if (command != null)
				m_CommandRetries.Remove(command);
			
			SamsungProResponse response = new SamsungProResponse(args.Response);
			switch (response.Command)
			{
				case POWER:
					if (response.Id != GetWallIdForPowerCommand())
						return;
					byte powerValue = response.Values[0];
					if (powerValue == 1)
						PowerState = ePowerState.PowerOn;
					else if (powerValue == 0)
						PowerState = ePowerState.PowerOff;
					return;

				case VOLUME:
					if (response.Id != GetWallIdForVolumeCommand())
						return;
					Volume = response.Values[0];
					return;

				case MUTE:
					if (response.Id != GetWallIdForVolumeCommand())
						return;
					byte muteValue = response.Values[0];
					if (muteValue == 1)
						IsMuted = true;
					else if (muteValue == 0)
						IsMuted = false;
					return;

				case INPUT:
					if (response.Id != GetWallIdForInputCommand())
						return;
					byte inputCode = response.Values[0];
					ActiveInput = s_InputMap.ContainsValue(inputCode)
									? s_InputMap.GetKey(inputCode)
									: s_InputPcMap.ContainsValue(inputCode)
										  ? s_InputPcMap.GetKey(inputCode)
										  : (int?)null;
					break;

				case LAUNCHER:
					if (response.Id != GetWallIdForInputCommand())
						return;
					switch (response.Subcommand)
					{
						case LAUNCHER_MODE:
							switch (response.Values[0])
							{
								case LAUNCHER_MODE_MAGIC_INFO:
									LauncherMode = eLauncherMode.MagicInfo;
									break;

								case LAUNCHER_MODE_URL_LAUNCHER:
									LauncherMode = eLauncherMode.Url;
									break;

								case LAUNCHER_MODE_MAGIC_IWB:
									LauncherMode = eLauncherMode.MagicIwb;
									break;
							}
							break;

						case LAUNCHER_ADDRESS:
							AbsoluteUri = StringUtils.ToString(response.Values);
							break;
					}
					break;
			}
		}

		/// <summary>
		/// Called when a command fails.
		/// </summary>
		/// <param name="args"></param>
		private void ParseError(SerialResponseEventArgs args)
		{
			if (args.Data == null)
				return;

			ISamsungProCommand command = args.Data as ISamsungProCommand;
			if (command == null)
				throw new InvalidOperationException("Unexpected command type");

			RetryCommand(command);
		}

		/// <summary>
		/// Retry the given command if it's not hit the retry counter
		/// </summary>
		/// <param name="command"></param>
		private void RetryCommand([NotNull] ISamsungProCommand command)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			int retries;
			m_CommandRetries.TryGetValue(command, out retries);

			if (retries >= MAX_RETRIES)
			{
				Logger.Log(eSeverity.Error, "Command {0} failed and hit max retries", StringUtils.ToHexLiteral(command.Serialize()));
				return;
			}

			retries++;
			m_CommandRetries[command] = retries;

			Logger.Log(eSeverity.Informational, "Command {0} failed, retry attempt {1}", StringUtils.ToHexLiteral(command.Serialize()), retries);
			SendCommand(command);
		}

		private void SetLauncherMode(eLauncherMode mode)
		{
			if (DisableLauncher)
				throw new InvalidOperationException("Launcher is Disabled");

			if (PowerState != ePowerState.PowerOn)
				return;

			byte[] modeData;
			s_LauncherModeMap.TryGetValue(mode, out modeData);

			SendCommand(new SamsungProCommand(LAUNCHER, GetWallIdForInputCommand(), LAUNCHER_MODE, modeData));
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			DisableLauncher = settings.DisableLauncher;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(T settings)
		{
			base.CopySettingsFinal(settings);

			settings.DisableLauncher = DisableLauncher;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			DisableLauncher = false;
		}

		#endregion

		#region Console

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Disable Launcher", DisableLauncher);
			if (!DisableLauncher)
				addRow("Launcher Url", AbsoluteUri);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
			{
				yield return command;
			}

			if (!DisableLauncher)
			{
				yield return
					new GenericConsoleCommand<string>("SetUrl", "Sets the URL to be used in launcher mode",
					                                  s => SetUrlLauncherSource(new Uri(s)));
				yield return
					new GenericConsoleCommand<eLauncherMode>("SetLauncherMode", "Sets the launcher mode - <MagicInfo, Url, MagicIwb>",
					                                         m => SetLauncherMode(m));
			}
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
