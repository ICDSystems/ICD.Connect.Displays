using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Mock.Devices
{
	/// <summary>
	/// Mock display device for testing control systems.
	/// </summary>
	public sealed class MockDisplayWithAudio : AbstractDevice<MockDisplayWithAudioSettings>, IDisplayWithAudio
	{
		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		public event EventHandler<DisplayPowerStateApiEventArgs> OnPowerStateChanged;

		/// <summary>
		/// Raised when the selected HDMI input changes.
		/// </summary>
		public event EventHandler<DisplayInputApiEventArgs> OnActiveInputChanged;

		/// <summary>
		/// Raised when the scaling mode changes.
		/// </summary>
		public event EventHandler<DisplayScalingModeApiEventArgs> OnScalingModeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<DisplayMuteApiEventArgs> OnMuteStateChanged;

		/// <summary>
		/// Raised when the volume control availability changes
		/// </summary>
		public event EventHandler<DisplayVolumeControlAvailableApiEventArgs> OnVolumeControlAvailableChanged;

		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		public event EventHandler<DisplayVolumeApiEventArgs> OnVolumeChanged;

		private ePowerState m_PowerState;
		private int? m_ActiveInput;
		private eScalingMode m_ScalingMode;

		private long m_WarmingTime;
		private long m_CoolingTime;

		private readonly SafeTimer m_WarmingTimer;
		private readonly SafeTimer m_CoolingTimer;

		private bool m_IsMuted;
		private float m_Volume;

		private float? m_VolumeSafetyMin;
		private float? m_VolumeSafetyMax;
		private float? m_VolumeDefault;
		private bool m_VolumeControlAvailable;

		private int? m_RequestedInput;
		private eScalingMode? m_RequestedScalingMode;
		private bool? m_RequestedMute;
		private float? m_RequestedVolume;

		#region Properties

		/// <summary>
		/// When true assume TX is successful even if a request times out.
		/// </summary>
		public bool Trust { get; set; }

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public ePowerState PowerState
		{
			get { return m_PowerState; }
			private set
			{
				if (value == m_PowerState)
					return;

				m_PowerState = value;

				Log(eSeverity.Informational, "Power set to {0}", m_PowerState);

				UpdateCachedVolumeControlAvalaibleState();

				long expectedDuration = 0;

				switch (value)
				{
					case ePowerState.Warming:
						expectedDuration = m_WarmingTime;
						break;
					case ePowerState.Cooling:
						expectedDuration = m_CoolingTime;
						break;

					case ePowerState.PowerOn:
						if (m_RequestedInput.HasValue)
							SetActiveInput(m_RequestedInput.Value);
						if (m_RequestedMute.HasValue)
						{
							if (m_RequestedMute.Value)
								MuteOn();
							else
								MuteOff();
						}
						if (m_RequestedScalingMode.HasValue)
							SetScalingMode(m_RequestedScalingMode.Value);
						if (m_RequestedVolume.HasValue)
							SetVolume(m_RequestedVolume.Value);
						break;
				}

				OnPowerStateChanged.Raise(this, new DisplayPowerStateApiEventArgs(m_PowerState, expectedDuration));
			}
		}

		/// <summary>
		/// Gets the current active input address.
		/// </summary>
		public int? ActiveInput
		{
			get { return m_ActiveInput; }
			private set
			{
				if (value == m_ActiveInput)
					return;

				int? oldInput = m_ActiveInput;
				m_ActiveInput = value;

				Log(eSeverity.Informational, "Active input set to {0}", m_ActiveInput == null ? "NULL" : m_ActiveInput.ToString());

				if (oldInput.HasValue)
					OnActiveInputChanged.Raise(this, new DisplayInputApiEventArgs(oldInput.Value, false));

				if (m_ActiveInput.HasValue)
					OnActiveInputChanged.Raise(this, new DisplayInputApiEventArgs(m_ActiveInput.Value, true));
			}
		}

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		public eScalingMode ScalingMode
		{
			get { return m_ScalingMode; }
			private set
			{
				if (value == m_ScalingMode)
					return;

				m_ScalingMode = value;

				Log(eSeverity.Informational, "Scaling mode set to {0}", StringUtils.NiceName(m_ScalingMode));

				OnScalingModeChanged.Raise(this, new DisplayScalingModeApiEventArgs(m_ScalingMode));
			}
		}

		/// <summary>
		/// Gets the raw volume of the display.
		/// </summary>
		public float Volume
		{
			get { return m_Volume; }
			private set
			{
				if (Math.Abs(value - m_Volume) < 0.01f)
					return;

				m_Volume = value;

				Log(eSeverity.Informational, "Raw volume set to {0}", m_Volume);

				// If the volume went outside of safe limits clamp the volume to a safe value.
				float safeVolume = MathUtils.Clamp(m_Volume, this.GetVolumeSafetyOrDeviceMin(), this.GetVolumeSafetyOrDeviceMax());
				if (Math.Abs(m_Volume - safeVolume) > 0.01f)
					SetVolume(safeVolume);

				OnVolumeChanged.Raise(this, new DisplayVolumeApiEventArgs(m_Volume));
			}
		}

		/// <summary>
		/// Gets the volume as a float represented from 0.0f (silent) to 1.0f (as loud as possible)
		/// </summary>
		public float VolumePercent { get; private set; }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool IsMuted
		{
			get { return m_IsMuted; }
			private set
			{
				if (value == m_IsMuted)
					return;

				m_IsMuted = value;

				Log(eSeverity.Informational, "Mute set to {0}", m_IsMuted);

				OnMuteStateChanged.Raise(this, new DisplayMuteApiEventArgs(m_IsMuted));
			}
		}

		/// <summary>
		/// Override if the display volume minimum is not 0.
		/// </summary>
		public float VolumeDeviceMin { get { return 0; } }

		/// <summary>
		/// Override if the display volume maximum is not 100.
		/// </summary>
		public float VolumeDeviceMax { get { return 100; } }

		/// <summary>
		/// Prevents the device from going below this volume.
		/// </summary>
		public float? VolumeSafetyMin
		{
			get { return m_VolumeSafetyMin; }
			set
			{
				if (value != null)
					value = Math.Max((float)value, VolumeDeviceMin);
				m_VolumeSafetyMin = value;
			}
		}

		/// <summary>
		/// Prevents the device from going above this volume.
		/// </summary>
		public float? VolumeSafetyMax
		{
			get { return m_VolumeSafetyMax; }
			set
			{
				if (value != null)
					value = Math.Min((float)value, VolumeDeviceMax);
				m_VolumeSafetyMax = value;
			}
		}

		/// <summary>
		/// The default volume to use when the display powers on.
		/// </summary>
		public float? VolumeDefault
		{
			get { return m_VolumeDefault; }
			set
			{
				if (value != null)
				{
					value = MathUtils.Clamp((float)value, this.GetVolumeSafetyOrDeviceMin(),
					                        this.GetVolumeSafetyOrDeviceMax());
				}

				m_VolumeDefault = value;
			}
		}

		/// <summary>
		/// Indicates if volume control is currently available or not
		/// </summary>
		public bool VolumeControlAvailable
		{
			get { return m_VolumeControlAvailable; }
			set
			{
				if (value == m_VolumeControlAvailable)
					return;

				m_VolumeControlAvailable = value;

				OnVolumeControlAvailableChanged.Raise(this, new DisplayVolumeControlAvailableApiEventArgs(VolumeControlAvailable));

				if (VolumeControlAvailable && VolumeDefault != null)
					SetVolume((float)VolumeDefault);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public MockDisplayWithAudio()
		{
			m_PowerState = ePowerState.PowerOff;

			m_WarmingTimer = SafeTimer.Stopped(WarmingComplete);
			m_CoolingTimer = SafeTimer.Stopped(CoolingComplete);

			Controls.Add(new DisplayRouteDestinationControl(this, 0));
			Controls.Add(new DisplayPowerDeviceControl(this, 1));
			Controls.Add(new DisplayVolumeDeviceControl(this, 2));
		}

		/// <summary>
		/// Clears resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnPowerStateChanged = null;
			OnActiveInputChanged = null;
			OnScalingModeChanged = null;
			OnMuteStateChanged = null;
			OnVolumeChanged = null;
			OnVolumeControlAvailableChanged = null;

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public void PowerOn()
		{
			if (PowerState == ePowerState.PowerOn || PowerState == ePowerState.Warming)
				return;

			m_CoolingTimer.Stop();

			if (m_WarmingTime > 0)
			{
				PowerState = ePowerState.Warming;
				m_WarmingTimer.Reset(m_WarmingTime);
			}
			else
				PowerState = ePowerState.PowerOn;
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public void PowerOff()
		{
			if (PowerState == ePowerState.PowerOff || PowerState == ePowerState.Cooling)
				return;

			m_WarmingTimer.Stop();

			if (m_CoolingTime > 0)
			{
				PowerState = ePowerState.Cooling;
				m_CoolingTimer.Reset(m_CoolingTime);
			}
			else
				PowerState = ePowerState.PowerOff;
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public void SetActiveInput(int address)
		{
			m_RequestedInput = address;

			if (PowerState != ePowerState.PowerOn)
				return;

			ActiveInput = address;
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public void SetScalingMode(eScalingMode mode)
		{
			m_RequestedScalingMode = mode;

			if (PowerState != ePowerState.PowerOn)
				return;

			ScalingMode = mode;
		}

		/// <summary>
		/// Enables mute.
		/// </summary>
		public void MuteOn()
		{
			m_RequestedMute = true;

			if (!VolumeControlAvailable)
				return;

			IsMuted = true;
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public void MuteOff()
		{
			m_RequestedMute = false;

			if (!VolumeControlAvailable)
				return;

			IsMuted = false;
		}

		/// <summary>
		/// Toggles mute.
		/// </summary>
		public void MuteToggle()
		{
			if (IsMuted)
				MuteOff();
			else
				MuteOn();
		}

		/// <summary>
		/// Sets the raw volume.
		/// </summary>
		/// <param name="raw"></param>
		public void SetVolume(float raw)
		{
			m_RequestedVolume = raw;

			if (!VolumeControlAvailable)
				return;

			// Set the volume
			float min = this.GetVolumeSafetyOrDeviceMin();
			float max = this.GetVolumeSafetyOrDeviceMax();

			raw = MathUtils.Clamp(raw, min, max);
			Volume = MathUtils.Clamp(raw, this.GetVolumeSafetyOrDeviceMin(), this.GetVolumeSafetyOrDeviceMax());

			// Update the volume percentage
			min = this.GetVolumeSafetyOrDeviceMin();
			max = this.GetVolumeSafetyOrDeviceMax();

			VolumePercent = Math.Abs(min - max) < 0.01f ? 1.0f : MathUtils.MapRange(min, max, 0.0f, 1.0f, raw);
		}

		/// <summary>
		/// Increments the raw volume.
		/// </summary>
		public void VolumeUpIncrement()
		{
			if (!VolumeControlAvailable)
				return;

			SetVolume(Volume + 1);
		}

		/// <summary>
		/// Decrements the raw volume.
		/// </summary>
		public void VolumeDownIncrement()
		{
			if (!VolumeControlAvailable)
				return;

			SetVolume(Volume - 1);
		}

		private bool GetVolumeControlAvailable()
		{
			return PowerState == ePowerState.PowerOn;
		}

		private void UpdateCachedVolumeControlAvalaibleState()
		{
			VolumeControlAvailable = GetVolumeControlAvailable();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(MockDisplayWithAudioSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_WarmingTime = settings.WarmingTime;
			m_CoolingTime = settings.CoolingTime;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_WarmingTimer.Stop();
			m_CoolingTimer.Stop();

			m_WarmingTime = 0;
			m_CoolingTime = 0;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(MockDisplayWithAudioSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.WarmingTime = m_WarmingTime;
			settings.CoolingTime = m_CoolingTime;
		}

		private void WarmingComplete()
		{
			PowerState = ePowerState.PowerOn;
		}

		private void CoolingComplete()
		{
			PowerState = ePowerState.PowerOff;
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in DisplayConsole.GetConsoleNodes(this))
				yield return node;

			foreach (IConsoleNodeBase node in DisplayWithAudioConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			DisplayConsole.BuildConsoleStatus(this, addRow);
			DisplayWithAudioConsole.BuildConsoleStatus(this, addRow);
			addRow("WarmingTime (ms)", m_WarmingTime);
			addRow("CoolingTime (ms)", m_CoolingTime);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in DisplayConsole.GetConsoleCommands(this))
				yield return command;

			foreach (IConsoleCommand command in DisplayWithAudioConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for the "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
