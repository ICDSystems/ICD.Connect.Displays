using System.Collections.Generic;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Devices.ProjectorScreens
{
	public sealed class RelayProjectorScreenDevice : AbstractProjectorScreenDevice<RelayProjectorScreenDeviceSettings>
	{
		private const bool DISPLAY_OFF_KEY = false;
		private const bool DISPLAY_ON_KEY = true;


		#region fields

		private bool m_RelayLatch;
		private int m_RelayHoldTime;

		/// <summary>
		/// Timer to reset all relays to open state
		/// </summary>
		private readonly SafeTimer m_ResetTimer;

		/// <summary>
		/// Dictionary that holds the relays for display on/off status
		/// Key is the power status for that relay
		/// false = off relay
		/// true = on relay
		/// </summary>
		private readonly Dictionary<bool, IRelayPort> m_DisplayRelays;

		#endregion

		#region Properties

		public bool RelayLatch
		{
			get
			{
				return m_RelayLatch;
			}
		}

		public int RelayHoldTime
		{
			get
			{
				return m_RelayHoldTime;
			}
		}

		public IRelayPort DisplayOffRelay
		{
			get
			{
				IRelayPort relay;
				if (m_DisplayRelays.TryGetValue(DISPLAY_OFF_KEY, out relay))
					return relay;
				return null;
			}
		}

		public IRelayPort DisplayOnRelay
		{
			get
			{
				IRelayPort relay;
				if (m_DisplayRelays.TryGetValue(DISPLAY_ON_KEY, out relay))
					return relay;
				return null;
			}
		}

		#endregion

		public RelayProjectorScreenDevice()
		{
			m_DisplayRelays = new Dictionary<bool, IRelayPort>();
			m_ResetTimer = SafeTimer.Stopped(OpenAllRelays);
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			if (!base.GetIsOnlineStatus())
				return false;

			// If any relays are specified but not online, offline
			foreach (IRelayPort relay in m_DisplayRelays.Values)
				if (!relay.IsOnline)
					return false;

			return true;
		}

		
		/// <summary>
		/// This is called when the reset timer expires - this opens all relays.
		/// </summary>
		private void OpenAllRelays()
		{
			foreach (IRelayPort relay in m_DisplayRelays.Values)
				relay.Open();
		}

		protected override void SetInitialState()
		{
			// If relays are latched and display is not null, latch relays
			if (RelayLatch && Display != null)
				ActivateDisplayRelays(Display.IsPowered);
		}

		private void SetDisplayOffRelay(IRelayPort relay)
		{
			if (relay == DisplayOffRelay)
				return;

			Unsubscribe(DisplayOffRelay);
			m_DisplayRelays[DISPLAY_OFF_KEY] = relay;
			Subscribe(DisplayOffRelay);
         
			UpdateCachedOnlineStatus();
		}

		private void SetDisplayOnRelay(IRelayPort relay)
		{
			if (relay == DisplayOnRelay)
				return;

			Unsubscribe(DisplayOnRelay);
			m_DisplayRelays[DISPLAY_ON_KEY] = relay;
			Subscribe(DisplayOnRelay);

			UpdateCachedOnlineStatus();
		}

		private void SetRelayHoldTime(int holdTime)
		{
			m_RelayHoldTime = holdTime;
		}

		#region Display Subscritpion/Callback

		protected override void DisplayOnIsPoweredChanged(object sender, DisplayPowerStateApiEventArgs args)
		{
			ActivateDisplayRelays(args.Data);
		}

		private void ActivateDisplayRelays(bool displayPower)
		{
			// Cancel reset timer, if in progress
			m_ResetTimer.Stop();

			IRelayPort activeRelay;
			IRelayPort complimentRelay;

			// Open Compliment Relay
			if (m_DisplayRelays.TryGetValue(!displayPower, out complimentRelay))
				complimentRelay.Open();

			// Close Active Relay
			if (m_DisplayRelays.TryGetValue(displayPower, out activeRelay))
				activeRelay.Close();

			// If not in latch mode, set a timer to open relays
			if (!RelayLatch)
				m_ResetTimer.Reset(RelayHoldTime);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(RelayProjectorScreenDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			// Off Relay
			IRelayPort displayOffRelay = null;
			if (settings.DisplayOffRelay != null)
			{
				try
				{
					displayOffRelay = factory.GetOriginatorById<IRelayPort>(settings.DisplayOffRelay.Value);
				}
				catch (KeyNotFoundException)
				{
					Log(eSeverity.Error, "No relay with id {0}", settings.DisplayOffRelay);
				}
				
			}
			SetDisplayOffRelay(displayOffRelay);

			// On Relay
			IRelayPort displayOnRelay = null;
			if (settings.DisplayOnRelay != null)
			{
				try
				{
					displayOnRelay = factory.GetOriginatorById<IRelayPort>(settings.DisplayOnRelay.Value);
				}
				catch (KeyNotFoundException)
				{
					Log(eSeverity.Error, "No relay with id {0}", settings.DisplayOnRelay);
				}
			}
			SetDisplayOnRelay(displayOnRelay);

			// Additional Parameters
			m_RelayLatch = settings.RelayLatch;
			m_RelayHoldTime = settings.RelayHoldTime;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(RelayProjectorScreenDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.DisplayOffRelay = DisplayOffRelay == null ? null : (int?)DisplayOffRelay.Id;
			settings.DisplayOnRelay = DisplayOnRelay == null ? null : (int?)DisplayOnRelay.Id;
			settings.RelayLatch = m_RelayLatch;
			settings.RelayHoldTime = m_RelayHoldTime;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();
			SetDisplayOffRelay(null);
			SetDisplayOnRelay(null);
			m_RelayLatch = false;
			m_RelayHoldTime = 500;
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
			addRow("Latch Relay", RelayLatch);
			addRow("Relay Hold Time", RelayHoldTime);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("ActivateRelays", "Activates relays for the given display power state, true for on false for off", b => ActivateDisplayRelays(b));
			yield return new ConsoleCommand("OpenRelays", "Opens all relays", () => OpenAllRelays());
			yield return new GenericConsoleCommand<int>("SetRelayHoldTime", "How long to hold relays closed, in ms", i => SetRelayHoldTime(i));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}