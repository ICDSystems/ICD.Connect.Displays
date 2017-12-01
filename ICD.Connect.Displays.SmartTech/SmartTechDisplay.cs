using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.SmartTech
{
    public sealed class SmartTechDisplay : AbstractDisplayWithAudio<SmartTechDisplaySettings>
    {
        #region Commands
        private const string POWER_ON = "set powerstate=on";
        private const string POWER_OFF = "set powerstate=standby";
        private const string POWER_RESPONSE = "powerstate=";

        private const string INPUT_HDMI1 = "set input=HDMI1";
        private const string INPUT_HDMI2 = "set input=HDMI2";
        private const string INPUT_RESPONSE = "input=";

        private const string ASPECT_REAL = "set aspectratio=real";
        private const string ASPECT_NORMAL = "set aspectratio=normal";
        private const string ASPECT_FULL = "set aspectratio=full";
        private const string ASPECT_WIDE = "set aspectratio=wide";
        private const string ASPECT_DYNAMIC = "set aspectratio=dynamic";
        private const string ASPECT_ZOOM = "set aspectratio=zoom";
        private const string ASPECT_RESPONSE = "aspectratio=";

        private const string VOLUME_UP = "set volume+1";
        private const string VOLUME_DOWN = "set volume-1";
        private const string VOLUME_SET = "set volume={0}";
        private const string VOLUME_RESPONSE = "volume=";

        private const string MUTE_ON = "set mute=on";
        private const string MUTE_OFF = "set mute=off";
        private const string MUTE_RESPONSE = "mute=";

        private const char CARR_RETURN = (char)0x0D;
        #endregion

        /// <summary>
        /// Maps scaling mode to command.
        /// </summary>
        private static readonly Dictionary<eScalingMode, string> s_ScalingModeMap =
            new Dictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, ASPECT_WIDE},
				{eScalingMode.Square4X3, ASPECT_NORMAL},
				{eScalingMode.NoScale, ASPECT_REAL},
				{eScalingMode.Zoom, ASPECT_ZOOM},
			};

        /// <summary>
        /// Maps index to an input command.
        /// </summary>
        private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
			{1, INPUT_HDMI1},
            {2, INPUT_HDMI2}
		};

        #region Methods
        /// <summary>
        /// Sets and configures the port for communication with the physical display.
        /// </summary>
        public void SetPort(ISerialPort port)
        {
            if (port is IComPort)
                ConfigureComPort(port as IComPort);

            ISerialBuffer buffer = new DelimiterSerialBuffer(CARR_RETURN);
            SerialQueue queue = new SerialQueue();
            queue.SetPort(port);
            queue.SetBuffer(buffer);
            queue.Timeout = 10 * 1000;

            SetSerialQueue(queue);

            if (port != null)
                QueryState();
        }

        /// <summary>
        /// Configures a com port for communication with the physical display.
        /// </summary>
        /// <param name="port"></param>
        [PublicAPI]
        public static void ConfigureComPort(IComPort port)
        {
            port.SetComPortSpec(eComBaudRates.ComspecBaudRate19200,
                                eComDataBits.ComspecDataBits8,
                                eComParityType.ComspecParityNone,
                                eComStopBits.ComspecStopBits1,
                                eComProtocolType.ComspecProtocolRS232,
                                eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                                eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                                false);
        }

        /// <summary>
        /// Gets the number of HDMI inputs.
        /// </summary>
        public override int InputCount { get { return s_InputMap.Count; } }

        public override void PowerOn()
        {
            SendNonFormattedCommand(POWER_ON);
        }

        public override void PowerOff()
        {
            SendNonFormattedCommand(POWER_OFF);
        }

        public override void SetHdmiInput(int address)
        {
            if (s_InputMap.ContainsKey(address))
            {
                SendNonFormattedCommand(s_InputMap[address]);
            }
        }

        public override void SetScalingMode(eScalingMode mode)
        {
            if (s_ScalingModeMap.ContainsKey(mode))
            {
                SendNonFormattedCommand(s_ScalingModeMap[mode]);
            }
        }

        /// <summary>
        /// Increments the raw volume.
        /// </summary>
        public override void VolumeUpIncrement()
        {
            SendNonFormattedCommand(VOLUME_UP, VolumeComparer);
        }

        /// <summary>
        /// Decrements the raw volume.
        /// </summary>
        public override void VolumeDownIncrement()
        {
            SendNonFormattedCommand(VOLUME_DOWN, VolumeComparer);
        }

        /// <summary>
        /// Sends the volume set command to the device after validation has been performed.
        /// </summary>
        /// <param name="raw"></param>
        protected override void VolumeSetRawFinal(float raw)
        {
            SendNonFormattedCommand(string.Format(VOLUME_SET, (int)raw), VolumeComparer);
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
        /// Called when a command times out.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
        {
            Log(eSeverity.Alert, "Command Timed Out: " + args.Data.Serialize());
        }

        /// <summary>
        /// Called when a command gets a response from the physical display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
        {
            Logger.AddEntry(eSeverity.Debug, "Recieved Response: {0}", args.Response);
	        var response = args.Response.Trim('\n', '\r', ' ', '>').ToLower();
            if (response.StartsWith(POWER_RESPONSE) ||
				response.StartsWith(ASPECT_RESPONSE) ||
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
            Logger.AddEntry(eSeverity.Debug, "Sending Command: {0}", data);
            SendCommand(new SerialData(data + CARR_RETURN), (a, b) => comparer(a.Serialize(), b.Serialize()));
        }

        /// <summary>
        /// Called when a command executes correctly.
        /// </summary>
        private void ParseSuccess(string response)
        {
            response = response.TrimEnd(CARR_RETURN);

            if (response.StartsWith(POWER_RESPONSE))
            {
                ParsePowerResponse(response);
            }
            else if (response.StartsWith(ASPECT_RESPONSE))
            {
                ParseAspectResponse(response);
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
                    IsPowered = true;
                    break;
                case "off":
                    IsPowered = false;
                    break;
                default:
                    LogUnexpectedResponse(response);
                    break;
            }
        }

        /// <summary>
        /// Handles responses for aspect states
        /// </summary>
        /// <param name="response"></param>
        private void ParseAspectResponse(string response)
        {
            switch (response.Substring(ASPECT_RESPONSE.Length).ToLower())
            {
                case "real":
                    LookupAspectAndSetScalingMode(ASPECT_REAL);
                    break;
                case "normal":
                    LookupAspectAndSetScalingMode(ASPECT_NORMAL);
                    break;
                case "full":
                    LookupAspectAndSetScalingMode(ASPECT_FULL);
                    break;
                case "wide":
                    LookupAspectAndSetScalingMode(ASPECT_WIDE);
                    break;
                case "dynamic":
                    LookupAspectAndSetScalingMode(ASPECT_DYNAMIC);
                    break;
                case "zoom":
                    LookupAspectAndSetScalingMode(ASPECT_ZOOM);
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
                    HdmiInput = 1;
                    break;
                case "hdmi2":
                    HdmiInput = 2;
                    break;
                default:
                    HdmiInput = null;
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
            Logger.AddEntry(eSeverity.Debug, "New Volume Is: {0}", response.Substring(VOLUME_RESPONSE.Length));
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
        /// Digs through the scaling mode dictionary for the correct aspect, and sets the scaling mode to the appropriate eScalingMode
        /// </summary>
        /// <param name="aspect">The matching aspect string in the dictionary to search for</param>
        private void LookupAspectAndSetScalingMode(string aspect)
        {
            ScalingMode = s_ScalingModeMap.ContainsValue(aspect)
                        ? s_ScalingModeMap.GetKey(aspect)
                        : eScalingMode.Unknown;
        }

        /// <summary>
        /// Called when a command fails.
        /// </summary>
        private void ParseError(string response)
        {
            Log(eSeverity.Error, response);
        }

        private void LogUnexpectedResponse(string response)
        {
            Log(eSeverity.Notice, "Unexpected reponse was returned: {0}", response);
        }


        #endregion

        #region Settings

        /// <summary>
        /// Override to apply properties to the settings instance.
        /// </summary>
        /// <param name="settings"></param>
        protected override void CopySettingsFinal(SmartTechDisplaySettings settings)
        {
            base.CopySettingsFinal(settings);

            if (SerialQueue != null && SerialQueue.Port != null)
                settings.Port = SerialQueue.Port.Id;
            else
                settings.Port = null;
        }

        /// <summary>
        /// Override to clear the instance settings.
        /// </summary>
        protected override void ClearSettingsFinal()
        {
            base.ClearSettingsFinal();

            SetPort(null);
        }

        /// <summary>
        /// Override to apply settings to the instance.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        protected override void ApplySettingsFinal(SmartTechDisplaySettings settings, IDeviceFactory factory)
        {
            base.ApplySettingsFinal(settings, factory);

            ISerialPort port = null;

            if (settings.Port != null)
            {
                port = factory.GetPortById((int)settings.Port) as ISerialPort;
                if (port == null)
                    Log(eSeverity.Error, "No Com Port with id {0}", settings.Port);
            }

            SetPort(port);
        }

        #endregion

    }
}