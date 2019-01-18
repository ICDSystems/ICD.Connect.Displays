using System;
using System.Collections.Generic;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Devices.ProjectorScreens
{
	public sealed class IrProjectorScreenDevice : AbstractProjectorScreenDevice<IrProjectorScreenDeviceSettings>
	{
		private const bool DISPLAY_OFF_KEY = false;
		private const bool DISPLAY_ON_KEY = true;


		#region fields

		private IIrPort m_IrPort;

		/// <summary>
		/// Dictionary that holds the relays for display on/off status
		/// Key is the power status for that relay
		/// false = off relay
		/// true = on relay
		/// </summary>
		private readonly Dictionary<bool, string> m_DisplayIrCommands;

		#endregion

		#region Properties

		public IIrPort IrPort
		{
			get { return m_IrPort; }
		}

		public String DisplayOnCommand
		{
			get
			{
				string command;
				return m_DisplayIrCommands.TryGetValue(DISPLAY_ON_KEY, out command) ? command : null;
			}
			set { m_DisplayIrCommands[DISPLAY_ON_KEY] = value; }
		}

		public String DisplayOffCommand
		{
			get
			{
				string command;
				return m_DisplayIrCommands.TryGetValue(DISPLAY_OFF_KEY, out command) ? command : null;
			}
			set { m_DisplayIrCommands[DISPLAY_OFF_KEY] = value; }
		}

		#endregion

		public IrProjectorScreenDevice()
		{
			m_DisplayIrCommands = new Dictionary<bool, string>();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			if (!base.GetIsOnlineStatus())
				return false;

			return IrPort != null && IrPort.IsOnline;
		}

		protected override void SetInitialState()
		{
		}

		#region Display Subscritpion/Callback

		protected override void DisplayOnIsPoweredChanged(object sender, DisplayPowerStateApiEventArgs args)
		{
			ActivateScreen(args.Data);
		}

		private void ActivateScreen(bool powerState)
		{
			if (IrPort == null)
				return;

			string command;
			if (!m_DisplayIrCommands.TryGetValue(powerState, out command))
				return;

			IrPort.PressAndRelease(command);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(IrProjectorScreenDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			IIrPort irPort = null;
			if (settings.IrPort != null)
				irPort = factory.GetOriginatorById<IIrPort>(settings.IrPort.Value);
			SetIrPort(irPort);

			if (settings.DisplayOnCommand != null)
				DisplayOnCommand = settings.DisplayOnCommand;

			if (settings.DisplayOffCommand != null)
				DisplayOffCommand = settings.DisplayOffCommand;
		}

		private void SetIrPort(IIrPort irPort)
		{
			Unsubscribe(IrPort);
			m_IrPort = irPort;
			Subscribe(IrPort);
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(IrProjectorScreenDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.IrPort = IrPort != null ? IrPort.Id : (int?)null;
			settings.DisplayOnCommand = DisplayOnCommand;
			settings.DisplayOffCommand = DisplayOffCommand;

		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();
			SetIrPort(null);
			DisplayOnCommand = null;
			DisplayOffCommand = null;
		}

		#endregion

		#region Console

		#endregion
	}
}