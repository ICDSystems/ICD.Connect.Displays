using System.Collections.Generic;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Devices.DisplayScreenRelayControl
{
	public sealed class DisplayScreenRelayControl : AbstractDevice<DisplayScreenRelayControlSettings>
	{

		private const bool DISPLAY_OFF_KEY = false;
		private const bool DISPLAY_ON_KEY = true;

		#region fields

		private IDisplay m_Display;
		private bool m_RelayLatch;
		private int m_RelayHoldTime;

		/// <summary>
		/// Dictionary that holds the relays for display on/off status
		/// Key is the power status for that relay
		/// false = off relay
		/// true = on relay
		/// </summary>
		private readonly Dictionary<bool, IRelayPort> m_DisplayRelays;

		/// <summary>
		/// Timer to reset all relays to open state
		/// </summary>
		private readonly SafeTimer m_ResetTimer;

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

		public DisplayScreenRelayControl()
		{
			m_DisplayRelays = new Dictionary<bool, IRelayPort>();
			m_ResetTimer = SafeTimer.Stopped(OpenAllRelays);
		}

		
		/// <summary>
		/// This is called when the reset timer expires - this opens all relays.
		/// </summary>
		private void OpenAllRelays()
		{
			foreach (IRelayPort relay in m_DisplayRelays.Values)
				relay.Open();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			// If there is no display, offline
			if (m_Display == null)
				return false;

			// If any relays are specified but not online, offline
			foreach (IRelayPort relay in m_DisplayRelays.Values)
				if (!relay.IsOnline)
					return false;

			return true;
		}

		private void SetDisplay(IDisplay display)
		{
			if (display == m_Display)
				return;

			Unsubscribe(m_Display);
			m_Display = display;
			Subscribe(m_Display);

			UpdateCachedOnlineStatus();
			
			// If relays are latched and display is not null, latch relays
			if (RelayLatch && m_Display != null)
				ActivateDisplayRelays(m_Display.IsPowered);
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

		#region Display Subscritpion/Callback

		private void Subscribe(IDisplay display)
		{
			if (display == null)
				return;

			display.OnIsPoweredChanged += DisplayOnIsPoweredChanged;
		}

		private void Unsubscribe(IDisplay display)
		{
			if (display == null)
				return;

			display.OnIsPoweredChanged -= DisplayOnIsPoweredChanged;

		}

		private void DisplayOnIsPoweredChanged(object sender, DisplayPowerStateApiEventArgs args)
		{
			ActivateDisplayRelays(args.Data);
		}

		public void ActivateDisplayRelays(bool displayPower)
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

		#region Relay Subscription/Callback

		private void Subscribe(IRelayPort relay)
		{
			if (relay == null)
				return;

			relay.OnIsOnlineStateChanged += RelayOnIsOnlineStateChanged;
		}

		private void Unsubscribe(IRelayPort relay)
		{
			if (relay == null)
				return;

			relay.OnIsOnlineStateChanged -= RelayOnIsOnlineStateChanged;
		}

		private void RelayOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetDisplay(null);
			SetDisplayOffRelay(null);
			SetDisplayOnRelay(null);
			m_RelayLatch = false;
			m_RelayHoldTime = 500;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(DisplayScreenRelayControlSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			if (settings.Display == null)
			{
				Logger.AddEntry(eSeverity.Error, "No display id set for DisplayScreenRelayControl: {0}", Name);
				return;
			}

			// Off Relay
			IRelayPort displayOffRelay = null;
			if (settings.DisplayOffRelay != null)
				displayOffRelay = factory.GetOriginatorById<IRelayPort>(settings.DisplayOffRelay.Value);
			SetDisplayOffRelay(displayOffRelay);

			// On Relay
			IRelayPort displayOnRelay = null;
			if (settings.DisplayOnRelay != null)
				displayOnRelay = factory.GetOriginatorById<IRelayPort>(settings.DisplayOnRelay.Value);
			SetDisplayOnRelay(displayOnRelay);

            // Additional Parameters
			m_RelayLatch = settings.RelayLatch;
			m_RelayHoldTime = settings.RelayHoldTime;

			// Display
			IDisplay display = null;
			if (settings.Display != null)
				display = factory.GetOriginatorById<IDisplay>(settings.Display.Value);
			SetDisplay(display);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(DisplayScreenRelayControlSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Display = m_Display == null ? null : (int?)m_Display.Id;
			settings.DisplayOffRelay = DisplayOffRelay == null ? null : (int?)DisplayOffRelay.Id;
			settings.DisplayOnRelay = DisplayOnRelay == null ? null : (int?)DisplayOnRelay.Id;
			settings.RelayLatch = m_RelayLatch;
			settings.RelayHoldTime = m_RelayHoldTime;
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

			yield return new GenericConsoleCommand<bool>("ActivateRelays", "Activates relays for the given display power state", (b) => ActivateDisplayRelays(b));
			yield return new ConsoleCommand("OpenRelays", "Opens all relays", () => OpenAllRelays());
			yield return new GenericConsoleCommand<int>("SetRelayHoldTime", "How long to hold relays closed, in ms", (i) => SetRelayHoldTime(i));
		}

		private void SetRelayHoldTime(int holdTime)
		{
			m_RelayHoldTime = holdTime;
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}