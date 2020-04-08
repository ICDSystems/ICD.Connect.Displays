using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Microsoft.Devices
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

        private const string INPUT_ONBOARD_PC = "Source=0\n";
        private const string INPUT_DISPLAYPORT = "Source=1\n";
        private const string INPUT_HDMI = "Source=2\n";
        private const string INPUT_VGA = "Source=3\n";

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
        private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>
		{
			{0, INPUT_ONBOARD_PC},
			{1, INPUT_DISPLAYPORT},
			{2, INPUT_HDMI},
			{3, INPUT_VGA }
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

	    /// <summary>
	    /// Configures a com port for communication with the physical display.
	    /// </summary>
	    /// <param name="port"></param>
	    public override void ConfigurePort(ISerialPort port)
	    {
		    base.ConfigurePort(port);

		    ISerialBuffer buffer = new DelimiterSerialBuffer((char)0x0A);
		    SerialQueue queue = new SerialQueue();
		    queue.SetPort(port);
		    queue.SetBuffer(buffer);
		    queue.Timeout = 10 * 1000;

		    SetSerialQueue(queue);
	    }

	    #region Methods

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
            SendNonFormattedCommand(s_InputMap.GetValue(address));
        }

	    /// <summary>
	    /// Increments the raw volume.
	    /// </summary>
	    public override void VolumeUpIncrement()
	    {
		    if (!VolumeControlAvailable)
			    return;
		    SendNonFormattedCommand(VOLUME_UP);
	    }

	    /// <summary>
	    /// Decrements the raw volume.
	    /// </summary>
	    public override void VolumeDownIncrement()
	    {
		    if (!VolumeControlAvailable)
			    return;
		    SendNonFormattedCommand(VOLUME_DOWN);
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
		    SendNonFormattedCommand(string.Format(VOLUME_SET, volume));
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

	    #endregion

	    #region SerialQueue Callbacks

	    /// <summary>
        /// Called when a command times out.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
        {
            Logger.Log(eSeverity.Error, "Command {0} timed out.", StringUtils.ToHexLiteral(args.Data.Serialize()));
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

		    if (command.StartsWith("Volume="))
		    {
			    command = command.Substring("Volume=".Length).Trim();
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
        /// Called when a command executes correctly.
        /// </summary>
        /// <param name="args"></param>
        private void ParseSuccess(SerialResponseEventArgs args)
	    {
		    string response = args.Data.Serialize();

		    switch (response)
		    {
			    case POWER_ON_RESPONSE:
				    PowerState = ePowerState.PowerOn;
				    break;
			    case POWER_OFF_RESPONSE:
				    PowerState = ePowerState.PowerOff;
				    break;
			    case INPUT_CHANGE_RESPONSE:
				    ActiveInput = 1;
				    break;
			    default:
				    if (response.StartsWith(VOLUME_CHANGE_RESPONSE))
				    {
					    Volume = int.Parse(response.Split(' ')[2]);
					    IsMuted = false;
				    }
				    else
				    {
						Logger.Log(eSeverity.Notice, "Unexpected reponse was returned: {0}", response);
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
			Logger.Log(eSeverity.Error, "Unexpected response: " + args.Response);
        }

	    #endregion
    }
}