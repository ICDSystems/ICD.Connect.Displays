using ICD.Common.Utils.Services.Logging;
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

	#region Methods

	#region Abstract Methods

	protected abstract void SetInitialState();
	protected abstract void DisplayOnIsPoweredChanged(object sender, DisplayPowerStateApiEventArgs args);

	#endregion

	#region Protected Methods

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

	private void SetDisplay(IDisplay display)
	{
		if (display == m_Display)
			return;

		Unsubscribe(m_Display);
		m_Display = display;
		Subscribe(m_Display);

		UpdateCachedOnlineStatus();
		SetInitialState();
	}

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
			Log(eSeverity.Error, "No display id set for {0}", this);
			return;
		}

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
	protected override void CopySettingsFinal(T settings)
	{
		base.CopySettingsFinal(settings);

		settings.Display = m_Display == null ? null : (int?)m_Display.Id;

	}

	#endregion
}
}