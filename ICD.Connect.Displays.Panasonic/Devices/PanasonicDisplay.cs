using System;
using System.Collections.Generic;
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
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Panasonic.Devices
{
    public sealed class PanasonicDisplay : AbstractDisplayWithAudio<PanasonicDisplaySettings>
	{
		#region Command Constants

		private const string START_MESSAGE = "\x02";
	    private const string END_MESSAGE = "\x03";

		//ASCII = ADZZ;
	    private const string COMMAND_PREFIX = "\x41\x44\x5A\x5A\x3B";
		//ASCII = ER
	    private const string ERROR_PREFIX = "\x45\x52";

	    private const string FAILURE_BUSY = START_MESSAGE + ERROR_PREFIX + "401" + END_MESSAGE;
	    private const string FAILURE_PARAMETER = START_MESSAGE + ERROR_PREFIX + "402" + END_MESSAGE;

		//ASCII = PON
		private const string POWER_ON = START_MESSAGE + COMMAND_PREFIX + "\x50\x4F\x4E" + END_MESSAGE;
		//ASCII = POF
		private const string POWER_OFF = START_MESSAGE + COMMAND_PREFIX + "\x50\x4F\x46" + END_MESSAGE;
		//ACII = QPW
		private const string QUERY_POWER = START_MESSAGE + COMMAND_PREFIX + "\x51\x50\x57" + END_MESSAGE;

		//ASCII = AMT:1
		private const string MUTE_ON = START_MESSAGE + COMMAND_PREFIX + "\x41\x4d\x54\x3a\x31" + END_MESSAGE;
		//ASCII = AMT:0
		private const string MUTE_OFF = START_MESSAGE + COMMAND_PREFIX + "\x41\x4d\x54\x3a\x30" + END_MESSAGE;

		//ASCII == AUU
		private const string VOLUME_UP = START_MESSAGE + COMMAND_PREFIX + "\x41\x55\x55" + END_MESSAGE;
		//ASCII == AUD
		private const string VOLUME_DOWN = START_MESSAGE + COMMAND_PREFIX + "\x41\x55\x44" + END_MESSAGE;
		//ASCII = QAV
		private const string QUERY_VOLUME = START_MESSAGE + COMMAND_PREFIX + "\x51\x41\x56" + END_MESSAGE;

		//ASCII = AVL:
		private const string VOLUME_SET_TEMPLATE = START_MESSAGE + COMMAND_PREFIX + "\x41\x56\x4c\x3a{0}" + END_MESSAGE;

		//ASCII = IIS:HD1
		private const string INPUT_HDMI = START_MESSAGE + COMMAND_PREFIX + "\x49\x49\x53\x3a\x48\x44\x31" + END_MESSAGE;
		//ASCII = QIN
		private const string QUERY_INPUT = START_MESSAGE + COMMAND_PREFIX + "\x51\x49\x4e" + END_MESSAGE;

		#endregion

	    #region Serial Queue Limit Constants

	    private const int RETRY_LIMIT = 10;

	    #endregion

	    #region Fields

	    /// <summary>
        /// Maps index to an input command.
        /// </summary>
		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, INPUT_HDMI}
		};

	    private readonly SafeCriticalSection m_RetrySection;
	    private readonly Dictionary<ISerialData, int> m_BusyCommandsToRetryCount;

	    #endregion

	    #region Properties

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

	    #region Constructor

	    /// <summary>
	    /// Constructor.
	    /// </summary>
	    public PanasonicDisplay()
	    {
		    m_RetrySection = new SafeCriticalSection();
		    m_BusyCommandsToRetryCount = new Dictionary<ISerialData, int>();
	    }

	    #endregion

	    #region Methods

	    /// <summary>
	    /// Configures the given port for communication with the device.
	    /// </summary>
	    /// <param name="port"></param>
	    public override void ConfigurePort(IPort port)
	    {
		    base.ConfigurePort(port);

		    ISerialBuffer buffer = new BoundedSerialBuffer(0x02, 0x03);
		    SerialQueue queue = new SerialQueue();
		    queue.SetPort(port as ISerialPort);
		    queue.SetBuffer(buffer);
		    queue.Timeout = 10 * 1000;

		    SetSerialQueue(queue);
	    }

	    /// <summary>
	    /// Polls the physical device for the current state.
	    /// </summary>
	    protected override void QueryState()
	    {
		    base.QueryState();
			SendNonFormattedCommand(QUERY_POWER);
		    if (PowerState != ePowerState.PowerOn)
			    return;

			SendNonFormattedCommand(QUERY_VOLUME);
			SendNonFormattedCommand(QUERY_INPUT);
		}

	    /// <summary>
	    /// Powers the TV.
	    /// </summary>
	    [PublicAPI]
        public override void PowerOn()
        {
            SendNonFormattedCommand(POWER_ON);
        }

	    /// <summary>
	    /// Shuts down the TV.
	    /// </summary>
	    [PublicAPI]
        public override void PowerOff()
        {
            SendNonFormattedCommand(POWER_OFF);
        }

        public override void MuteOn()
        {
            SendNonFormattedCommand(MUTE_ON);
        }

        public override void MuteOff()
        {
            SendNonFormattedCommand(MUTE_OFF);
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

	    public override void VolumeUpIncrement()
        {
			if (!VolumeControlAvailable)
				return;
            SendNonFormattedCommand(VOLUME_UP);
            SendNonFormattedCommand(QUERY_VOLUME);
        }

        public override void VolumeDownIncrement()
        {
			if (!VolumeControlAvailable)
				return;
            SendNonFormattedCommand(VOLUME_DOWN);
            SendNonFormattedCommand(QUERY_VOLUME);
        }

        protected override void SetVolumeFinal(float raw)
        {
			if (!VolumeControlAvailable)
				return;

	        int volume = (int)Math.Round(raw);
            string setVolCommand = GenerateSetVolumeCommand(volume);
            SendNonFormattedCommand(setVolCommand);
        }

        public override void SetActiveInput(int address)
        {
            SendNonFormattedCommand(s_InputMap.GetValue(address));
        }

        /// <summary>
        /// Returns the 3 character command
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
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
	    /// Called when a command is sent to the physical display.
	    /// </summary>
	    /// <param name="sender"></param>
	    /// <param name="args"></param>
	    protected override void SerialQueueOnSerialTransmission(object sender, SerialTransmissionEventArgs args)
	    {
		    if (!Trust)
			    return;

		    string command = args.Data.Serialize();

		    if (s_InputMap.ContainsValue(command))
		    {
			    ActiveInput = s_InputMap.GetKey(command);
			    return;
		    }

		    switch (command)
		    {
			    case MUTE_ON:
				    IsMuted = true;
				    return;

				case MUTE_OFF:
				    IsMuted = false;
				    return;
		    }

			// Volume set "\x02ADZZ;AVL:{0}\x03";
		    if (command.StartsWith("\x02ADZZ;AVL:"))
		    {
			    command = command.Replace("\x02ADZZ;AVL:", string.Empty)
			                     .Replace("\x03", string.Empty)
			                     .Trim();

			    Volume = int.Parse(command);
		    }
	    }

	    /// <summary>
        /// Called when a command gets a response from the physical display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
	    {
		    switch (args.Response)
		    {
			    case FAILURE_PARAMETER:
			    case FAILURE_BUSY:
				    ParseError(args);
				    break;
			    default:
				    ParseSuccess(args);
					ResetRetryCount(args.Data);
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
			Logger.Log(eSeverity.Error, "Command {0} timed out. Retrying...", StringUtils.ToHexLiteral(args.Data.Serialize()));
			RetryCommand(args.Data);
		}

        /// <summary>
        /// Called when a command is successful.
        /// </summary>
        /// <param name="args"></param>
        private void ParseSuccess(SerialResponseEventArgs args)
        {
	        string response = args.Response;
	        if (response.Length >= 4)
	        {
		        string command = ExtractCommand(response);
		        switch (command)
		        {
			        case "PON":
						PowerState = ePowerState.PowerOn;
				        return;
			        case "POF":
						PowerState = ePowerState.PowerOff;
				        return;
			        case "AMT":
				        string param = ExtractParameter(response, 1);
				        IsMuted = param == "1";
				        return;
			        case "IIS":
				        ActiveInput = ExtractParameter(response, 3) == "HD1" ? 1 : (int?)null;
				        return;
			        case "QAV":
				        float newVol;
				        var parameter = ExtractParameter(response, 3);
				        if (StringUtils.TryParse(parameter, out newVol))
					        Volume = newVol;
				        else
					        throw new InvalidOperationException(string.Format("Unable to parse {0} as volume float", parameter));
				        return;
		        }
	        }

	        ParseQueryResponse(args);
        }

		/// <summary>
		/// It seems on some Panasonic displays respond to queries with just the value queried
		/// This code looks at the responses and at the commands they were paired with and tries to decode them
		/// </summary>
		/// <param name="args"></param>
	    private void ParseQueryResponse(SerialResponseEventArgs args)
		{
		    if (args.Data == null)
			    return;

			string response = RemoveStxEtx(args.Response);

		    try
		    {
			    switch (args.Data.Serialize())
			    {
				    case QUERY_POWER:
						PowerState = int.Parse(response) == 1 ? ePowerState.PowerOn : ePowerState.PowerOff;
					    break;
					case QUERY_INPUT:
					    ActiveInput = response == "HD1" ? 1 : (int?)null;
					    break;
			    }
		    }
		    catch (Exception e)
		    {
				Logger.Log(eSeverity.Error, "Exception parsing unmatches response: {0}:{1} - {2}{3}", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()), StringUtils.ToMixedReadableHexLiteral(response), e.GetType(), e.Message);
		    }
	    }

	    private string RemoveStxEtx(string response)
	    {
		    return response.Substring(1, response.Length - 2);
	    }

        private static string GenerateSetVolumeCommand(int volumePercent)
        {
            volumePercent = MathUtils.Clamp(volumePercent, 0, 100);
            return string.Format(VOLUME_SET_TEMPLATE, volumePercent);
        }

        /// <summary>
        /// Called when a command fails.
        /// </summary>
        /// <param name="args"></param>
        private void ParseError(SerialResponseEventArgs args)
        {
	        switch (args.Response)
	        {
		        case FAILURE_BUSY:
			        Logger.Log(eSeverity.Error, "Error 401 Busy. Command {0} failed.",
			                   StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
			        RetryCommand(args.Data);
			        break;
		        case FAILURE_PARAMETER:
			        Logger.Log(eSeverity.Error, "Error 402 Invalid Parameter. Command {0} failed.",
			                   StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
			        break;
		        default:
			        Logger.Log(eSeverity.Error, "Error Unknown. Command {0} failed.",
			                   StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
			        break;
	        }
        }

        /// <summary>
        /// Called when a command fails with: ER401 Busy
        /// Times out after 10 retries.
        /// </summary>
        /// <param name="command"></param>
        private void RetryCommand(ISerialData command)
        {
	        IncrementRetryCount(command);
	        if (GetRetryCount(command) <= RETRY_LIMIT)
		        SendCommandPriority(command, (a, b) => a.Equals(b), 0);
	        else
	        {
		        Logger.Log(eSeverity.Error, "Command {0} failed too many times and hit the retry limit.",
		                   StringUtils.ToMixedReadableHexLiteral(command.Serialize()));
		        ResetRetryCount(command);
	        }
        }

		/// <summary>
		/// Gets the retry count for the given command.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
	    private int GetRetryCount(ISerialData command)
	    {
		    m_RetrySection.Enter();

		    try
		    {
			    return m_BusyCommandsToRetryCount.ContainsKey(command) ? m_BusyCommandsToRetryCount[command] : 0;
		    }
		    finally
		    {
			    m_RetrySection.Leave();
		    }
	    }

		/// <summary>
		/// Increments the retry count for the given command.
		/// </summary>
		/// <param name="command"></param>
	    private void IncrementRetryCount(ISerialData command)
	    {
		    m_RetrySection.Enter();

		    try
		    {
			    if (m_BusyCommandsToRetryCount.ContainsKey(command))
				    m_BusyCommandsToRetryCount[command]++;
				else
					m_BusyCommandsToRetryCount.Add(command, 1);
		    }
		    finally
		    {
			    m_RetrySection.Leave();
		    }
	    }

		/// <summary>
		/// Resets the retry count for the given command.
		/// </summary>
		/// <param name="command"></param>
	    private void ResetRetryCount(ISerialData command)
	    {
		    m_RetrySection.Enter();

		    try
		    {
			    m_BusyCommandsToRetryCount.Remove(command);
		    }
		    finally
		    {
			    m_RetrySection.Leave();
		    }
	    }

        #endregion
    }
}
