using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Microsoft.SurfaceHub.Devices
{
    public sealed class SurfaceHubDisplay : AbstractDisplayWithAudio<SurfaceHubDisplaySettings>
    {
        private const string POWER_ON = "PowerOn\n";
        private const string POWER_OFF = "PowerOff\n";

        private const string VOLUME_UP = "Volume+\n";
        private const string VOLUME_DOWN = "Volume-\n";
        private const string VOLUME_SET = "Volume={0}\n";

        private const string MUTE_ON = "AudioMute+\n";
        private const string MUTE_OFF = "AudioMute-\n";

        private const string INPUT_HDMI = "Source=2\n";

        private const string ERROR_UNKNOWN_OPERATOR = "Error: Unknown operator";
        private const string ERROR_UNKNOWN_COMMAND = "Error: Unknown command";
        private const string ERROR_UNKNOWN_PARAMETER = "Error: Unknown parameter";
        private const string ERROR_UNAVAILABLE = "Error: Command not available when off";

        private const string POWER_ON_RESPONSE = "Power=5";
        private const string POWER_OFF_RESPONSE = "Power=0";
        private const string VOLUME_CHANGE_RESPONSE = "Volume = ";
        private const string INPUT_CHANGE_RESPONSE = "Source = 2";

        /// <summary>
        /// Maps index to an input command.
        /// </summary>
        private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
			{1, INPUT_HDMI},
		};

        /// <summary>
        /// Gets the number of HDMI inputs.
        /// </summary>
        public override int InputCount { get { return s_InputMap.Count; } }

        /// <summary>
        /// Configures a com port for communication with the physical display.
        /// </summary>
        /// <param name="port"></param>
        [PublicAPI]
		public override void ConfigureComPort(IComPort port)
        {
			base.ConfigureComPort(port);
            port.SetComPortSpec(eComBaudRates.ComspecBaudRate115200,
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
            SendNonFormattedCommand(POWER_ON);
        }

        public override void PowerOff()
        {
            SendNonFormattedCommand(POWER_OFF);
        }

        public override void SetHdmiInput(int address)
        {
            SendNonFormattedCommand(s_InputMap[address]);
        }

        public override void SetScalingMode(eScalingMode mode)
        {
            //Do Nothing, Scaling Not Supported On Device
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
        /// Called when a command gets a response from the physical display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
        {
            if (args.Response.StartsWith(ERROR_UNAVAILABLE) ||
                args.Response.StartsWith(ERROR_UNKNOWN_COMMAND) ||
                args.Response.StartsWith(ERROR_UNKNOWN_OPERATOR) ||
                args.Response.StartsWith(ERROR_UNKNOWN_PARAMETER))
            {
                ParseError(args);
            }
            else
                ParseSuccess(args);
        }

        /// <summary>
        /// Increments the raw volume.
        /// </summary>
        public override void VolumeUpIncrement()
        {
            if (!IsPowered)
                return;
            SendNonFormattedCommand(VOLUME_UP);
        }

        /// <summary>
        /// Decrements the raw volume.
        /// </summary>
        public override void VolumeDownIncrement()
        {
            if (!IsPowered)
                return;
            SendNonFormattedCommand(VOLUME_DOWN);
        }

        /// <summary>
        /// Sends the volume set command to the device after validation has been performed.
        /// </summary>
        /// <param name="raw"></param>
        protected override void VolumeSetRawFinal(float raw)
        {
            if (!IsPowered)
                return;
            SendNonFormattedCommand(string.Format(VOLUME_SET, (int)raw));
        }

        /// <summary>
        /// Enables mute.
        /// </summary>
        public override void MuteOn()
        {
            SendNonFormattedCommand(MUTE_ON);
            IsMuted = true;
        }

        /// <summary>
        /// Disables mute.
        /// </summary>
        public override void MuteOff()
        {
            SendNonFormattedCommand(MUTE_OFF);
            IsMuted = false;
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
        /// Called when a command executes correctly.
        /// </summary>
        /// <param name="args"></param>
        private void ParseSuccess(SerialResponseEventArgs args)
        {
            string response = args.Data.Serialize();

            if (response == POWER_ON_RESPONSE)
                IsPowered = true;
            else if (response == POWER_OFF_RESPONSE)
                IsPowered = false;
            else if (response == INPUT_CHANGE_RESPONSE)
                HdmiInput = 1;
            else if (response.StartsWith(VOLUME_CHANGE_RESPONSE))
            {
                Volume = int.Parse(response.Split(' ')[2]);
                IsMuted = false;
            }
            else
            {
                Logger.AddEntry(eSeverity.Notice, "Unexpected reponse was returned: {0}", response);
            }
        }

        /// <summary>
        /// Called when a command fails.
        /// </summary>
        /// <param name="args"></param>
        private void ParseError(SerialResponseEventArgs args)
        {
            Log(eSeverity.Error,"Unexpected response: " + args.Response);
        }

        /// <summary>
        ///     Sets and configures the port for communication with the physical display.
        /// </summary>
        protected override void ConfigurePort(ISerialPort port)
        {
            if (port is IComPort)
                ConfigureComPort(port as IComPort);

            ISerialBuffer buffer = new DelimiterSerialBuffer((char)0x0A);
            SerialQueue queue = new SerialQueue();
            queue.SetPort(port);
            queue.SetBuffer(buffer);
            queue.Timeout = 10 * 1000;

            SetSerialQueue(queue);
        }
    }
}