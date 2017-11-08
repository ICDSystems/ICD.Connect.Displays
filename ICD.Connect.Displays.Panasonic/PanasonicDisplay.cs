﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Panasonic
{
    public sealed class PanasonicDisplay : AbstractDisplayWithAudio<PanasonicDisplaySettings>
    {
        private const string FAILURE_BUSY = "\x02ER401\x03";
        private const string FAILURE_PARAMETER = "\x02ER402\x03";

        private const string POWER_ON = "\x02ADZZ;PON\x03";
        private const string POWER_OFF = "\x02ADZZ;POF\x03";

        private const string MUTE_ON = "\x02ADZZ;AMT:1\x03";
        private const string MUTE_OFF = "\x02ADZZ;AMT:0\x03";

        private const string VOLUME_UP = "\x02ADZZ;AUU\x03";
        private const string VOLUME_DOWN = "\x02ADZZ;AUD\x03";

        private const string VOLUME_SET_TEMPLATE = "\x02ADZZ;AVL:{0}\x03";

        private const string INPUT_HDMI = "\x02ADZZ;IIS:HD1\x03";

        private const string ASPECT_AUTO = "\x02ADZZ;VSE:0\x03";
        private const string ASPECT_4_X3 = "\x02ADZZ;VSE:1\x03";
        private const string ASPECT_16_X9 = "\x02ADZZ;VSE:2\x03";
        private const string ASPECT_NATIVE = "\x02ADZZ;VSE:5\x03";
        private const string ASPECT_FULL = "\x02ADZZ;VSE:6\x03";

        /// <summary>
        /// Maps scaling mode to command.
        /// </summary>
        private static readonly Dictionary<eScalingMode, string> s_ScalingModeMap =
            new Dictionary<eScalingMode, string>
			{
				{eScalingMode.Wide16X9, ASPECT_16_X9},
				{eScalingMode.Square4X3, ASPECT_4_X3},
				{eScalingMode.NoScale, ASPECT_NATIVE},
				{eScalingMode.Zoom, ASPECT_AUTO}
			};

        /// <summary>
        /// Maps index to an input command.
        /// </summary>
        private static readonly Dictionary<int, string> s_InputMap = new Dictionary<int, string>
		{
            {1, INPUT_HDMI},
		};

        #region Properties

        /// <summary>
        /// Gets the number of HDMI inputs.
        /// </summary>
        public override int InputCount { get { return s_InputMap.Count; } }

        #endregion


        #region Methods

        /// <summary>
        /// Sets and configures the port for communication with the physical display.
        /// </summary>
        public void SetPort(ISerialPort port)
        {
            if (port is IComPort)
                ConfigureComPort(port as IComPort);

            ISerialBuffer buffer = new PanasonicDisplaySerialBuffer();
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
            SendNonFormattedCommand(VOLUME_UP);
        }

        public override void VolumeDownIncrement()
        {
            SendNonFormattedCommand(VOLUME_DOWN);
        }

        protected override void VolumeSetRawFinal(float raw)
        {
            SendNonFormattedCommand(GenerateSetVolumeCommand((int)Math.Floor(raw)));
        }

        public override void SetHdmiInput(int address)
        {
            SendNonFormattedCommand(s_InputMap[address]);
        }

        public override void SetScalingMode(eScalingMode mode)
        {
            SendNonFormattedCommand(s_ScalingModeMap[mode]);
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
            SendNonFormattedCommand(data, (a, b) => a==b);
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
            string response = args.Data.Serialize();
            string command = ExtractCommand(response);

            switch (command)
            {
                case "PON":
                    IsPowered = true;
                    break;
                case "POF":
                    IsPowered = false;
                    break;
                case "AUU":
                    Volume++;
                    IsMuted = false;
                    break;
                case "AUD":
                    Volume--;
                    IsMuted = false;
                    break;
                case "AMT":
                    string param = ExtractParameter(response, 1);
                    IsMuted = param == "1";
                    break;
                case "IIS":
                    HdmiInput = ExtractParameter(response, 3) == "HD1" ? 1 : (int?)null;
                    break;
                case "VSE":
                    ScalingMode = GetScalingMode(ExtractParameter(response, 1));
                    break;
            }
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
                    {
                        return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap[k] == ASPECT_AUTO);
                    }
                    break;
                case "1":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_4_X3))
                    {
                        return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap[k] == ASPECT_4_X3);
                    }
                    break;
                case "2":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_16_X9))
                    {
                        return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap[k] == ASPECT_16_X9);
                    }
                    break;
                case "5":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_NATIVE))
                    {
                        return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap[k] == ASPECT_NATIVE);
                    }
                    break;
                case "6":
                    if (s_ScalingModeMap.ContainsValue(ASPECT_FULL))
                    {
                        return s_ScalingModeMap.Keys.First(k => s_ScalingModeMap[k] == ASPECT_FULL);
                    }
                    break;
            }
            return eScalingMode.Unknown;
        }

        private static string GenerateSetVolumeCommand(int volumePercent)
        {
            if (volumePercent > 100 || volumePercent < 0)
            {
                throw new ArgumentOutOfRangeException("volumePercent", "The volume value must be between 0 and 100, inclusive.");
            }
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
                    Log(eSeverity.Error, "Error 401 Busy. Command {0} failed.", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
                    break;
                case FAILURE_PARAMETER:
                    Log(eSeverity.Error, "Error 402 Invalid Parameter. Command {0} failed.", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
                    break;
                default:
                    Log(eSeverity.Error, "Error Unknown. Command {0} failed.", StringUtils.ToMixedReadableHexLiteral(args.Data.Serialize()));
                    break;
            }
        }

        #endregion
    }
}