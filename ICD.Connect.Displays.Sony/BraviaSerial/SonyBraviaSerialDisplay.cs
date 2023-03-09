using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Sony.BraviaSerial
{
    public sealed class SonyBraviaSerialDisplay : AbstractDisplayWithAudio<SonyBraviaSerialDisplaySettings>
    {
        private static readonly BiDictionary<int, string> s_InputMap = new BiDictionary<int, string>()
        {
            {1, "\x04\x01"}, // HDMI 1
            {2, "\x04\x02"}, // HDMI 2
            {3, "\x04\x03"}, // HDMI 3
            {4, "\x04\x04"}, // HDMI 4
            {5, "\x04\x05"}, // HDMI 5
            {11, "\x05\x01"}, //PC
            {21, "\x03\x01"}, //Component 1
            {22, "\x03\x02"}, //Component 2
            {23, "\x03\x03"}, //Component 3
            {31, "\x02\x01"}, // Video 1
            {32, "\x02\x02"}, // Video 2
            {33, "\x02\x03"}, // Video 3
            {41, "\x07\x01"}, // Shared Input
        };
        
        #region Properties
        
        /// <summary>
        /// Returns the features that are supported by this display.
        /// </summary>
        public override eVolumeFeatures SupportedVolumeFeatures
        {
            get
            {
                return eVolumeFeatures.Mute | eVolumeFeatures.MuteFeedback | eVolumeFeatures.MuteAssignment |
                       eVolumeFeatures.Volume | eVolumeFeatures.VolumeFeedback | eVolumeFeatures.VolumeAssignment;
            }
        }

        /// <summary>
        /// Override if the display volume minimum is not 0.
        /// </summary>
        public override float VolumeDeviceMin
        {
            get { return 0; }
        }

        /// <summary>
        /// Override if the display volume maximum is not 100.
        /// </summary>
        public override float VolumeDeviceMax
        {
            get { return 100; }
        }
        
        #endregion
        
        #region Public Methods

        /// <summary>
        /// Sets and configures the port for communication with the physical display.
        /// </summary>
        public override void ConfigurePort(IPort port)
        {
            base.ConfigurePort(port);

            port.DefaultDebugMode = eDebugMode.Hex;

            SerialQueue queue = new SerialQueue();
            ISerialBuffer buffer = new SonyBraviaSerialBuffer(queue);
            queue.SetPort(port as ISerialPort);
            queue.SetBuffer(buffer);
            queue.Timeout = 20 * 1000;

            SetSerialQueue(queue);

            ISerialPort serialPort = port as ISerialPort;
            if (serialPort != null && serialPort.IsConnected)
                QueryState();
        }
        
        /// <summary>
        /// Powers the TV.
        /// </summary>
        public override void PowerOn()
        {
            SendCommand(SonyBraviaSerialCommand.SetPower(true));
        }

        /// <summary>
        /// Shuts down the TV.
        /// </summary>
        public override void PowerOff()
        {
            SendCommand(SonyBraviaSerialCommand.SetStandby(true));
            SendCommand(SonyBraviaSerialCommand.SetPower(false));
        }

        /// <summary>
        /// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
        /// </summary>
        /// <param name="address"></param>
        public override void SetActiveInput(int address)
        {
            string data;
            if (!s_InputMap.TryGetValue(address, out data))
                throw new ArgumentOutOfRangeException("address", string.Format("No input at address {0}", address));
            
            SendCommand(SonyBraviaSerialCommand.SetInput(data.ToCharArray()));
        }
        
        /// <summary>
        /// Increments the raw volume.
        /// </summary>
        public override void VolumeUpIncrement()
        {
            SendCommand(SonyBraviaSerialCommand.IncrementVolume(true));
        }

        /// <summary>
        /// Decrements the raw volume.
        /// </summary>
        public override void VolumeDownIncrement()
        {
            SendCommand(SonyBraviaSerialCommand.IncrementVolume(false));
        }
        
        /// <summary>
        /// Enables mute.
        /// </summary>
        public override void MuteOn()
        {
            SendCommand(SonyBraviaSerialCommand.SetMute(true));
        }

        /// <summary>
        /// Disables mute.
        /// </summary>
        public override void MuteOff()
        {
            SendCommand(SonyBraviaSerialCommand.SetMute(false));
        }
        
        /// <summary>
        /// Toggles mute.
        /// Uses direct display toggle command
        /// </summary>
        public override void MuteToggle()
        {
            SendCommand(SonyBraviaSerialCommand.ToggleMute());
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

        #region Protected Overrides
        
        /// <summary>
        /// Called when a command is sent to the physical display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void SerialQueueOnSerialTransmission(object sender, SerialTransmissionEventArgs args)
        {
            if (!Trust)
                return;
            
            throw new NotImplementedException("Trust mode isn't implemented yet");
        }

        /// <summary>
        /// Called when a command gets a response from the physical display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
        {
            SonyBraviaSerialResponse response;
            try
            {
                response = SonyBraviaSerialResponse.FromResponse(args.Response);
            }
            catch (ArgumentException e)
            {
                Logger.Log(eSeverity.Error, "Error Parsing response: {0}", e.Message);
                return;
            }

            SonyBraviaSerialCommand command = args.Data as SonyBraviaSerialCommand;
            if (command == null)
                return;

            // Standby has no parsing associated with it
            if (command.CommandFunction == SonyBraviaSerialCommand.eCommandFunction.Standby)
                return;

            switch (command.CommandType)
            {
                case SonyBraviaSerialCommand.eCommandType.Query:
                    HandleResponseQuery(command, response);
                    break;
                case SonyBraviaSerialCommand.eCommandType.Control:
                    HandleResponseControl(command, response);
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
            // Todo: Actually handle timeouts probably
            IcdConsole.PrintLine(eConsoleColor.YellowOnRed, "Queue Timeout on command: {0}", StringUtils.ToHexLiteral(args.Data.Serialize()) );
        }

        /// <summary>
        /// Sends the volume set command to the device after validation has been performed.
        /// </summary>
        /// <param name="raw"></param>
        protected override void SetVolumeFinal(float raw)
        {
            SendCommand(SonyBraviaSerialCommand.SetVolume((int)raw));
        }

        

        /// <summary>
        /// Called when the state of IsMuted changes
        /// </summary>
        protected override void HandleIsMutedChanged(bool isMuted)
        {
            base.HandleIsMutedChanged(isMuted);
            
            if (!isMuted)
                PollVolume();
        }
        
        /// <summary>
        /// Polls the physical device for the current state.
        /// </summary>
        protected override void QueryState()
        {
            base.QueryState();
            PollPower();

            if (PowerState != ePowerState.PowerOn)
                return;

            PollInput();
            PollMute();
            PollVolume();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Poll the power state
        /// </summary>
        private void PollPower()
        {
            SendCommand(SonyBraviaSerialCommand.GetQuery(SonyBraviaSerialCommand.eCommandFunction.Power));
        }

        /// <summary>
        /// Poll the input state
        /// </summary>
        private void PollInput()
        {
            SendCommand(SonyBraviaSerialCommand.GetQuery(SonyBraviaSerialCommand.eCommandFunction.InputSelect));
        }

        /// <summary>
        /// Poll the volume state
        /// </summary>
        private void PollVolume()
        {
            SendCommand(SonyBraviaSerialCommand.GetQuery(SonyBraviaSerialCommand.eCommandFunction.VolumeControl));
        }

        /// <summary>
        /// Poll the mute state
        /// </summary>
        private void PollMute()
        {
            SendCommand(SonyBraviaSerialCommand.GetQuery(SonyBraviaSerialCommand.eCommandFunction.Muting));
        }
        
        /// <summary>
        /// Handles command responses
        /// Sets appropriate states, or polls states
        /// </summary>
        /// <param name="command"></param>
        /// <param name="response"></param>
        private void HandleResponseControl(SonyBraviaSerialCommand command, SonyBraviaSerialResponse response)
        {
            if (response.Answer != SonyBraviaSerialResponse.eAnswer.Completed)
                return;
            
            char[] commandData = command.Data.ToArray();

            switch (command.CommandFunction)
            {
                case SonyBraviaSerialCommand.eCommandFunction.Power:
                    PowerState = commandData[0] == SonyBraviaSerialCommand.DATA_OFF ? ePowerState.PowerOff : ePowerState.PowerOn;
                    break;
                case SonyBraviaSerialCommand.eCommandFunction.InputSelect:
                    // Check for toggle command vs set command
                    if (commandData[0] == SonyBraviaSerialCommand.DATA_TOGGLE)
                    {
                        PollInput();
                    }
                    else
                    {
                        string inputCode = new string(commandData);
                        int input;
                        if (s_InputMap.TryGetKey(inputCode, out input))
                            ActiveInput = input;
                    }
                    break;
                case SonyBraviaSerialCommand.eCommandFunction.VolumeControl:
                    // Any volume command unmutes
                    IsMuted = false;
                    // Check for toggle command vs set command
                    switch (commandData[0])
                    {
                        case SonyBraviaSerialCommand.DATA_TOGGLE:
                            PollVolume();
                            break;
                        case SonyBraviaSerialCommand.DATA_DIRECT:
                            Volume = commandData[1];
                            break;
                    }

                    break;
                
                case SonyBraviaSerialCommand.eCommandFunction.Muting:
                    // Check for toggle command vs set command
                    switch (commandData[0])
                    {
                        case SonyBraviaSerialCommand.DATA_TOGGLE:
                            PollMute();
                            break;
                        case SonyBraviaSerialCommand.DATA_DIRECT:
                            IsMuted = commandData[1] != SonyBraviaSerialCommand.DATA_OFF;
                            break;
                    }
                    break;
            }
            
        }

        /// <summary>
        /// Handles query responses, and updates appropriate states
        /// </summary>
        /// <param name="command"></param>
        /// <param name="response"></param>
        private void HandleResponseQuery(SonyBraviaSerialCommand command, SonyBraviaSerialResponse response)
        {
            if (response.Answer != SonyBraviaSerialResponse.eAnswer.Completed)
                return;

            char[] responseData = response.Data.ToArray();

            switch (command.CommandFunction)
            {
                case SonyBraviaSerialCommand.eCommandFunction.Power:
                    PowerState = responseData[0] == SonyBraviaSerialCommand.DATA_OFF ? ePowerState.PowerOff : ePowerState.PowerOn;
                    break;
                case SonyBraviaSerialCommand.eCommandFunction.InputSelect:
                    string inputCode = new string(responseData);
                    int input;
                    if (s_InputMap.TryGetKey(inputCode, out input))
                        ActiveInput = input;
                    else
                        Logger.Log(eSeverity.Alert, "No input found for code {0}", StringUtils.ToHexLiteral(inputCode));
                    break;
                case SonyBraviaSerialCommand.eCommandFunction.VolumeControl:
                    // IsMuted returns 0 volume level
                    if (IsMuted && responseData[1] == 0)
                        return;
                    Volume = responseData[1];
                    break;
                case SonyBraviaSerialCommand.eCommandFunction.Muting:
                    IsMuted = responseData[1] != SonyBraviaSerialCommand.DATA_OFF;
                    break;
            }
        }
        
        #endregion
        
        #region Console

        /// <summary>
        /// Gets the child console commands.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IConsoleCommand> GetConsoleCommands()
        {
            foreach (var command in GetBaseConsoleCommands())
                yield return command;

            yield return new GenericConsoleCommand<bool>("BufferDebug", "Enable or disable buffer debug mode",
                b => SetBufferDebug(b));
            yield return new ConsoleCommand("PollPower", "", () => PollPower());
            yield return new ConsoleCommand("PollInput", "", () => PollInput());
            yield return new ConsoleCommand("PollVolume", "", () => PollVolume());
            yield return new ConsoleCommand("PollMute", "", () => PollMute());
        }

        private void SetBufferDebug(bool state)
        {
            if (SerialQueue == null)
                return;
            
            var buffer = SerialQueue.Buffer as SonyBraviaSerialBuffer;

            if (buffer == null)
                return;
            
            buffer.Debug = state;
        }

        private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
        {
            return base.GetConsoleCommands();
        }
        
        #endregion
    }
}