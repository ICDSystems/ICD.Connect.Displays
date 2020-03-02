using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
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

		//ASCII = VSE:0
		private const string ASPECT_AUTO = START_MESSAGE + COMMAND_PREFIX + "\x56\x53\x45\x3a\x30" + END_MESSAGE;
		//ASCII = VSE:1
		private const string ASPECT_4_X3 = START_MESSAGE + COMMAND_PREFIX + "\x56\x53\x45\x3a\x31" + END_MESSAGE;
		//ASCII = VSE:2
		private const string ASPECT_16_X9 = START_MESSAGE + COMMAND_PREFIX + "\x56\x53\x45\x3a\x32" + END_MESSAGE;
		//ASCII = VSE:5
		private const string ASPECT_NATIVE = START_MESSAGE + COMMAND_PREFIX + "\x56\x53\x45\x3a\x35" + END_MESSAGE;
		//ASCII = VSE:6
		private const string ASPECT_FULL = START_MESSAGE + COMMAND_PREFIX + "\x56\x53\x45\x3a\x36" + END_MESSAGE;
		//ASCII = QSE
		private const string QUERY_ASPECT = START_MESSAGE + COMMAND_PREFIX + "\x51\x53\x45" + END_MESSAGE;
		#endregion

		/// <summary>
		/// Maps scaling mode to command.
		/// </summary>
		private static readonly BiDictionary<eScalingMode, string> s_ScalingModeMap =
			new BiDictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, ASPECT_16_X9},
				{eScalingMode.Square4X3, ASPECT_4_X3},
				{eScalingMode.NoScale, ASPECT_NATIVE},
				{eScalingMode.Zoom, ASPECT_AUTO}
			};

        /// <summary>
        /// Maps index to an input command.
        /// </summary>
		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, INPUT_HDMI}
		};

        #region Properties

        #endregion

        #region Methods

        /// <summary>
        /// Sets and configures the port for communication with the physical display.
        /// </summary>
        protected override void ConfigurePort(ISerialPort port)
        {
            if (port is IComPort)
                ConfigureComPort(port as IComPort);

            ISerialBuffer buffer = new BoundedSerialBuffer(0x02, 0x03);
            SerialQueue queue = new SerialQueue();
            queue.SetPort(port);
            queue.SetBuffer(buffer);
            queue.Timeout = 10 * 1000;

            SetSerialQueue(queue);
        }

        /// <summary>
        /// Configures a com port for communication with the physical display.
        /// </summary>
        /// <param name="port"></param>
        [PublicAPI]
		public override void ConfigureComPort(IComPort port)
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

	    protected override void QueryState()
	    {
		    base.QueryState();
			SendNonFormattedCommand(QUERY_POWER);
		    if (!IsPowered)
			    return;

			SendNonFormattedCommand(QUERY_VOLUME);
			SendNonFormattedCommand(QUERY_INPUT);
		    SendNonFormattedCommand(QUERY_ASPECT);
		}

        [PublicAPI]
        public override void PowerOn()
        {
            SendNonFormattedCommand(POWER_ON);
        }

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

        public override void VolumeUpIncrement()
        {
            if (!IsPowered)
                return;
            SendNonFormattedCommand(VOLUME_UP);
            SendNonFormattedCommand(QUERY_VOLUME);
        }

        public override void VolumeDownIncrement()
        {
            if (!IsPowered)
                return;
            SendNonFormattedCommand(VOLUME_DOWN);
            SendNonFormattedCommand(QUERY_VOLUME);
        }

        protected override void VolumeSetRawFinal(float raw)
        {
            if (!IsPowered)
                return;
            string setVolCommand = GenerateSetVolumeCommand((int)raw);
            SendNonFormattedCommand(setVolCommand);
        }

        public override void SetActiveInput(int address)
        {
            SendNonFormattedCommand(s_InputMap.GetValue(address));
        }

        public override void SetScalingMode(eScalingMode mode)
        {
            SendNonFormattedCommand(s_ScalingModeMap.GetValue(mode));
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

		    if (s_ScalingModeMap.ContainsValue(command))
		    {
			    ScalingMode = s_ScalingModeMap.GetKey(command);
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
            if (args.Response == FAILURE_BUSY || args.Response == FAILURE_PARAMETER)
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
            Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToHexLiteral(args.Data.Serialize()));
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
				        IsPowered = true;
				        return;
			        case "POF":
				        IsPowered = false;
				        return;
			        case "AMT":
				        string param = ExtractParameter(response, 1);
				        IsMuted = param == "1";
				        return;
			        case "IIS":
				        ActiveInput = ExtractParameter(response, 3) == "HD1" ? 1 : (int?)null;
				        return;
			        case "VSE":
				        ScalingMode = GetScalingMode(ExtractParameter(response, 1));
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
				IcdConsole.PrintLine(eConsoleColor.Magenta, "ParsingQueryResponse - Command:{0} - Response:{1}", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()), StringUtils.ToMixedReadableHexLiteral(response));
			    switch (args.Data.Serialize())
			    {
				    case QUERY_POWER:
					    IsPowered = int.Parse(response) == 1;
					    break;
					case QUERY_INPUT:
					    ActiveInput = response == "HD1" ? 1 : (int?)null;
					    break;
			    }
		    }
		    catch (Exception e)
		    {
				Log(eSeverity.Error, "Exception parsing unmatches response: {0}:{1} - {2}{3}", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()), StringUtils.ToMixedReadableHexLiteral(response), e.GetType(), e.Message);
		    }
	    }

	    private string RemoveStxEtx(string response)
	    {
		    return response.Substring(1, response.Length - 2);
	    }

	    /// <summary>
        /// Attempts to match a scaling mode to the listed scaling modes in the dictionary.
        /// </summary>
        /// <param name="parameterAsString"></param>
        /// <returns></returns>
        private static eScalingMode GetScalingMode(string parameterAsString)
        {
            switch (parameterAsString)
            {
                case "0":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_AUTO))
                        return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap.GetValue(k) == ASPECT_AUTO);
                    break;
                case "1":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_4_X3))
						return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap.GetValue(k) == ASPECT_4_X3);
                    break;
                case "2":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_16_X9))
						return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap.GetValue(k) == ASPECT_16_X9);
                    break;
                case "5":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_NATIVE))
						return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap.GetValue(k) == ASPECT_NATIVE);
                    break;
                case "6":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_FULL))
						return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap.GetValue(k) == ASPECT_FULL);
                    break;
            }
            return eScalingMode.Unknown;
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
                    Log(eSeverity.Error, "Error 401 Busy. Command {0} failed.",
                        StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
                    break;
                case FAILURE_PARAMETER:
                    Log(eSeverity.Error, "Error 402 Invalid Parameter. Command {0} failed.",
                        StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
                    break;
                default:
                    Log(eSeverity.Error, "Error Unknown. Command {0} failed.",
                        StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
                    break;
            }
        }
        #endregion
    }
}
