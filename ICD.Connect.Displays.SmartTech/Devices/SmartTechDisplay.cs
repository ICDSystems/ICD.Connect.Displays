using System;
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
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.SmartTech.Devices
{
    public sealed class SmartTechDisplay : AbstractDisplayWithAudio<SmartTechDisplaySettings>
    {
        #region Commands

        private const string POWER_ON = "set powerstate=on";
        private const string POWER_OFF = "set powerstate=standby";
        private const string POWER_RESPONSE = "powerstate=";
        private const string POWER_GET = "get powerstate";

        // The inputs are literally the ones listed on the display when changing channel
        // VGA1, VGA2, DVI, DPORT, DVD/HD, S-VIDEO, VIDEO
        private const string INPUT_HDMI1 = "set input=HDMI1";
        private const string INPUT_HDMI2 = "set input=HDMI2";
        private const string INPUT_HDMI3 = "set input=HDMI3/PC";
        private const string INPUT_RESPONSE = "input=";
        private const string INPUT_GET = "get input";

        private const string VOLUME_UP = "set volume+1";
        private const string VOLUME_DOWN = "set volume-1";
        private const string VOLUME_SET = "set volume={0}";
        private const string VOLUME_RESPONSE = "volume=";
        private const string VOLUME_GET = "get volume";

        private const string MUTE_ON = "set mute=on";
        private const string MUTE_OFF = "set mute=off";
        private const string MUTE_RESPONSE = "mute=";
        private const string MUTE_GET = "get mute";

        private const char CARR_RETURN = (char)0x0D;

        #endregion

        /// <summary>
        /// Maps index to an input command.
        /// </summary>
		private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{1, INPUT_HDMI1},
            {2, INPUT_HDMI2},
			{3, INPUT_HDMI3}
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
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
        public override void ConfigurePort(IPort port)
        {
	        base.ConfigurePort(port);

            ISerialBuffer buffer = new DelimiterSerialBuffer(CARR_RETURN);
            SerialQueue queue = new SerialQueue();
            queue.SetPort(port as ISerialPort);
            queue.SetBuffer(buffer);
            queue.Timeout = 10 * 1000;

            SetSerialQueue(queue);

			ISerialPort serialPort = port as ISerialPort;
			if (serialPort != null && serialPort.IsConnected)
				QueryState();
        }

        protected override void QueryState()
        {
            base.QueryState();
            SendNonFormattedCommand(POWER_GET);

            if (PowerState != ePowerState.PowerOn)
                return;

            SendNonFormattedCommand(INPUT_GET);
            SendNonFormattedCommand(VOLUME_GET);
            SendNonFormattedCommand(MUTE_GET);
        }

	    public override void PowerOn()
        {
            SendNonFormattedCommand(POWER_ON);
        }

        public override void PowerOff()
        {
            SendNonFormattedCommand(POWER_OFF);
        }

        public override void SetActiveInput(int address)
        {
	        if (s_InputMap.ContainsKey(address))
		        SendNonFormattedCommand(s_InputMap.GetValue(address));
        }

	    /// <summary>
        /// Increments the raw volume.
        /// </summary>
        public override void VolumeUpIncrement()
        {
            if (!VolumeControlAvailable)
                return;
            SendNonFormattedCommand(VOLUME_UP, VolumeComparer);
        }

        /// <summary>
        /// Decrements the raw volume.
        /// </summary>
        public override void VolumeDownIncrement()
        {
            if (!VolumeControlAvailable)
                return;
            SendNonFormattedCommand(VOLUME_DOWN, VolumeComparer);
        }

        /// <summary>
        /// Sends the volume set command to the device after validation has been performed.
        /// </summary>
        /// <param name="raw"></param>
        protected override void SetVolumeFinal(float raw)
        {
            if (!VolumeControlAvailable)
                return;

	        int volume = (int)Math.Round(raw);
			SendNonFormattedCommand(string.Format(VOLUME_SET, volume), VolumeComparer);
        }

        /// <summary>
        /// Enables mute.
        /// </summary>
        public override void MuteOn()
        {
            SendNonFormattedCommand(MUTE_ON);
        }

        /// <summary>
        /// Disables mute.
        /// </summary>
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

	    /// <summary>
        /// Called when a command times out.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
        {
			Logger.Log(eSeverity.Alert, "Command Timed Out: " + args.Data.Serialize());
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

			// Strip the carriage return
		    command = command.TrimEnd(CARR_RETURN);

		    switch (command)
		    {
			    case POWER_ON:
				    PowerState = ePowerState.PowerOn;
				    return;

				case POWER_OFF:
				    PowerState = ePowerState.PowerOff;
					return;

				case MUTE_ON:
				    IsMuted = true;
					return;

				case MUTE_OFF:
				    IsMuted = false;
					return;
		    }

		    if (s_InputMap.ContainsValue(command))
		    {
			    ActiveInput = s_InputMap.GetKey(command);
			    return;
		    }

			// Volume set "set volume={0}"
		    if (command.StartsWith("set volume="))
		    {
			    command = command.Replace("set volume=", string.Empty).Trim();
			    Volume = int.Parse(command);
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
            string response = args.Response.Trim().Trim('>').Trim().ToLower();
            if (StringUtils.IsNullOrWhitespace(response))
            {
                return;
            }
            if (response.StartsWith(POWER_RESPONSE) ||
                response.StartsWith(INPUT_RESPONSE) ||
                response.StartsWith(VOLUME_RESPONSE) ||
                response.StartsWith(MUTE_RESPONSE))
            {
                ParseSuccess(response);
            }
            else
                ParseError(response);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Prevents multiple volume commands from being queued.
        /// </summary>
        /// <param name="commandA"></param>
        /// <param name="commandB"></param>
        /// <returns></returns>
        private static bool VolumeComparer(string commandA, string commandB)
        {
            return (commandA.StartsWith(VOLUME_UP) ||
                    commandA.StartsWith(VOLUME_DOWN) ||
                    commandA.StartsWith(VOLUME_SET))
                && (commandB.StartsWith(VOLUME_UP) ||
                    commandB.StartsWith(VOLUME_DOWN) ||
                    commandB.StartsWith(VOLUME_SET));
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
            SendCommand(new SerialData(data + CARR_RETURN), (a, b) => comparer(a.Serialize(), b.Serialize()));
        }

        /// <summary>
        /// Called when a command executes correctly.
        /// </summary>
        /// <param name="response"></param>
        private void ParseSuccess(string response)
        {
            response = response.TrimEnd(CARR_RETURN);

            if (response.StartsWith(POWER_RESPONSE))
            {
                ParsePowerResponse(response);
            }
            else if (response.StartsWith(INPUT_RESPONSE))
            {
                ParseInputResponse(response);
            }
            else if (response.StartsWith(VOLUME_RESPONSE))
            {
                ParseVolumeResponse(response);
            }
            else if (response.StartsWith(MUTE_RESPONSE))
            {
                ParseMuteResponse(response);
            }
        }

        /// <summary>
        /// Handles responses for power states
        /// </summary>
        private void ParsePowerResponse(string response)
        {
            switch (response.Substring(POWER_RESPONSE.Length).ToLower())
            {
                case "on":
                    PowerState = ePowerState.PowerOn;
                    break;
                case "off":
                    PowerState = ePowerState.PowerOff;
                    break;
                default:
                    LogUnexpectedResponse(response);
                    break;
            }
        }

        /// <summary>
        /// Handles responses for input states
        /// </summary>
        /// <param name="response"></param>
        private void ParseInputResponse(string response)
        {
            switch (response.Substring(INPUT_RESPONSE.Length).ToLower())
            {
                case "hdmi1":
                    ActiveInput = 1;
                    break;
                case "hdmi2":
                    ActiveInput = 2;
                    break;
                case "hdmi3/pc":
                    ActiveInput = 3;
                    break;
                default:
                    ActiveInput = null;
                    LogUnexpectedResponse(response);
                    break;
            }
        }

        /// <summary>
        /// Handle responses for volume states
        /// </summary>
        /// <param name="response"></param>
        private void ParseVolumeResponse(string response)
        {
            Volume = int.Parse(response.Substring(VOLUME_RESPONSE.Length));
            IsMuted = false;
        }

        /// <summary>
        /// Handle responses for mute states
        /// </summary>
        /// <param name="response"></param>
        private void ParseMuteResponse(string response)
        {
            IsMuted = response.Substring(MUTE_RESPONSE.Length).ToLower() == "on";
        }

        /// <summary>
        /// Called when a command fails.
        /// </summary>
        private void ParseError(string response)
        {
			Logger.Log(eSeverity.Error, response);
        }

        private void LogUnexpectedResponse(string response)
        {
			Logger.Log(eSeverity.Notice, "Unexpected reponse was returned: {0}", response);
        }

        #endregion
    }
}
