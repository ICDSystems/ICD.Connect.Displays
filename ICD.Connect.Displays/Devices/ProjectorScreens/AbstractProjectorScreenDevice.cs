using System;
using System.Collections.Generic;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Devices.ProjectorScreens
{
	public abstract class AbstractProjectorScreenDevice<T> : AbstractDevice<T>
		where T : AbstractProjectorScreenDeviceSettings, new()
	{
		#region Fields

		private IDisplay m_Display;

		#endregion

		#region Properties

		protected IDisplay Display { get { return m_Display; } }

		#endregion

		protected AbstractProjectorScreenDevice()
		{
			OnSettingsApplied += BaseOnSettingsApplied;
		}

		#region Methods

		#region Abstract Methods

		/// <summary>
		/// Set the initial state after the device is loaded
		/// This is to get the screen to the correct state before any events are received
		/// </summary>
		protected virtual void SetInitialState()
		{
		}

		/// <summary>
		/// Called when the display's power state changes
		/// This should activate the screen up/down actions
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected abstract void DisplayOnPowerStateChanged(object sender, DisplayPowerStateApiEventArgs args);

		#endregion

		#region Protected Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);
			OnSettingsApplied -= BaseOnSettingsApplied;
			SetDisplay(null);
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

			return true;
		}

		#region Port Subscription/Callback

		protected void Subscribe(IPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged += ProtOnIsOnlineStateChanged;
		}

		protected void Unsubscribe(IPort port)
		{
			if (port == null)
				return;

			port.OnIsOnlineStateChanged -= ProtOnIsOnlineStateChanged;
		}

		protected virtual void ProtOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#endregion

		#region Private Methods

		private void BaseOnSettingsApplied(object sender, EventArgs eventArgs)
		{
			SetInitialState();
		}

		private void SetDisplay(IDisplay display)
		{
			if (display == m_Display)
				return;

			Unsubscribe(m_Display);
			m_Display = display;
			Subscribe(m_Display);

			UpdateCachedOnlineStatus();
		}

		private void Subscribe(IDisplay display)
		{
			if (display == null)
				return;

			display.OnPowerStateChanged += DisplayOnPowerStateChanged;
		}

		private void Unsubscribe(IDisplay display)
		{
			if (display == null)
				return;

			display.OnPowerStateChanged -= DisplayOnPowerStateChanged;

		}

		#endregion

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetDisplay(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			if (settings.Display == null)
			{
				Logger.Log(eSeverity.Error, "No display id set for {0}", this);
				return;
			}

			// Display
			IDisplay display = null;
			if (settings.Display != null)
			{
				try
				{
					display = factory.GetOriginatorById<IDisplay>(settings.Display.Value);
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No display with id {0}", settings.Display);
				}
			}
			SetDisplay(display);
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(T settings)
		{
			base.CopySettingsFinal(settings);

			settings.Display = m_Display == null ? null : (int?)m_Display.Id;
		}

		#endregion

		#region Console

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Display", Display);
		}

		#endregion
	}
}