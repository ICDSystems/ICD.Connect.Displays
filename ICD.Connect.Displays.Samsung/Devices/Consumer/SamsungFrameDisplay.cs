using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	public sealed class SamsungFrameDisplay : AbstractSamsungDisplay<SamsungFrameDisplaySettings>
	{
		public event EventHandler<BoolEventArgs> OnArtModeChanged;

		private bool m_ArtMode;

		public bool ArtMode
		{
			get
			{
				return m_ArtMode;
			}
			private set
			{
				if (m_ArtMode == value)
					return;

				m_ArtMode = value;

				OnArtModeChanged.Raise(this, value);
			}
		}

		public bool ArtModeDefault { get; private set; }

		public bool? ArtModeAtPowerOn { get; private set; }

		public void SetArtMode(bool artMode)
		{
			if (ArtMode == artMode)
				return;

			ArtMode = artMode;

			
			if(artMode && PowerState == ePowerState.PowerOff)
			{
				// If display off and art mode set, turn on the display power and set it to art mode
				base.PowerOn();
				SendNonFormattedCommand(SamsungCommand.ART_MODE_ON, CommandComparer, SamsungCommand.PRIORITY_POWER_INITIAL);
			} 
			else if (!artMode && PowerState == ePowerState.PowerOff)
			{
				//If display off and art mode unset, turn off display power
				base.PowerOff();
			}
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public override void PowerOn()
		{
			if (ArtMode)
				SendNonFormattedCommand(SamsungCommand.ART_MODE_OFF, CommandComparer, SamsungCommand.PRIORITY_POWER_INITIAL);
			
			// Still send the regular power on command, in case the display is powered off
			base.PowerOn();
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public override void PowerOff()
		{
			// In art mode, don't call the base PowerOff, because that will power the display off, not put it in art mode
			if (ArtMode)
			{
				if (SerialQueue == null)
					return;
				Logger.Log(eSeverity.Debug, "Display Power Off while {0} commands were enqueued. Commands dropped.",
				           SerialQueue.CommandCount);
				SerialQueue.Clear();

				SendNonFormattedCommand(SamsungCommand.ART_MODE_ON, CommandComparer, SamsungCommand.PRIORITY_POWER_INITIAL);
			}
			else
			{
				base.PowerOff();
			}
		}

		#region Private Methods

		/// <summary>
		/// Called when a command is successful.
		/// </summary>
		/// <param name="args"></param>
		protected override void ParseSuccess(SerialResponseEventArgs args)
		{
			if (args.Data == null)
				return;

			string command = RemoveCheckSum(args.Data.Serialize());

			switch (command)
			{
				case SamsungCommand.ART_MODE_ON:
					PowerState = ePowerState.PowerOff;
					PowerRetries = 0;
					return;
				case SamsungCommand.ART_MODE_OFF:
					PowerState = ePowerState.PowerOn;
					PowerRetries = 0;
					return;
			}

			base.ParseSuccess(args);


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

			string command = RemoveCheckSum(args.Data.Serialize());

			switch (command)
			{
				case SamsungCommand.ART_MODE_OFF:
					PowerState = ePowerState.PowerOn;
					return;

				case SamsungCommand.ART_MODE_ON:
					PowerState = ePowerState.PowerOff;
					return;
			}

			base.SerialQueueOnSerialTransmission(sender, args);
		}

		protected override void HandlePowerStateChanged(ePowerState state)
		{
			base.HandlePowerStateChanged(state);

			if (state == ePowerState.PowerOn && ArtModeAtPowerOn.HasValue)
				ArtMode = ArtModeAtPowerOn.Value;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SamsungFrameDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ArtModeDefault = settings.ArtModeDefault;
			ArtModeAtPowerOn = settings.ArtModeAtPowerOn;

			
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SamsungFrameDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.ArtModeDefault = ArtModeDefault;
			settings.ArtModeAtPowerOn = ArtModeAtPowerOn;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ArtModeDefault = false;
			ArtModeAtPowerOn = null;
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			ArtMode = ArtModeDefault;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);
			addRow("ArtMode", ArtMode);
			addRow("ArtMode Default", ArtModeDefault);
			addRow("ArtMode at Power On", ArtModeAtPowerOn);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetArtMode", "Sets art mode true/false", a => SetArtMode(a));
			yield return
				new GenericConsoleCommand<bool>("SetArtModeAtPwrOn", "Sets the art mode at power on true/false",
				                                a => ArtModeAtPowerOn = a);
			yield return new ConsoleCommand("ClearArtModeAtPowerOn", "Clears art mode at power on setting", () => ArtModeAtPowerOn = null);
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}