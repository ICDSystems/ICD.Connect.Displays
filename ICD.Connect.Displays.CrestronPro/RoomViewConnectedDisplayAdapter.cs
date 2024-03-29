﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Misc.CrestronPro.Utils;
using ICD.Connect.Settings;
#if !NETSTANDARD
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronConnected;
using ICD.Connect.Misc.CrestronPro;
using ICD.Connect.Misc.CrestronPro.Extensions;
#endif
using ICD.Common.Properties;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Common.Logging.LoggingContexts;

namespace ICD.Connect.Displays.CrestronPro
{
	public sealed class RoomViewConnectedDisplayAdapter : AbstractDevice<RoomViewConnectedDisplaySettings>, IDisplayWithAudio
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
		/// Raised when the volume changes.
		/// </summary>
		public event EventHandler<DisplayVolumeApiEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<DisplayMuteApiEventArgs> OnMuteStateChanged;

		public event EventHandler<DisplayVolumeControlAvailableApiEventArgs> OnVolumeControlAvailableChanged;

#if !NETSTANDARD
		private RoomViewConnectedDisplay m_Display;
#endif

		private ePowerState m_PowerState;
		private int? m_ActiveInput;
		private float m_Volume;
		private bool m_IsMuted;
		private bool m_VolumeControlAvailable;

		#region Properties

		/// <summary>
		/// When true assume TX is successful even if a request times out.
		/// </summary>
		public bool Trust { get { return false; } set { throw new NotSupportedException(); } }

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

				Logger.LogSetTo(eSeverity.Informational, "PowerState", m_PowerState);

				OnPowerStateChanged.Raise(this, new DisplayPowerStateApiEventArgs(m_PowerState));

				UpdateCachedVolumeControlAvailableState();
			}
		}

		/// <summary>
		/// Gets the current hdmi input address.
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

				Logger.LogSetTo(eSeverity.Informational, "ActiveInput", m_ActiveInput);

				if (oldInput.HasValue)
					OnActiveInputChanged.Raise(this, new DisplayInputApiEventArgs(oldInput.Value, false));

				if (m_ActiveInput.HasValue)
					OnActiveInputChanged.Raise(this, new DisplayInputApiEventArgs(m_ActiveInput.Value, true));
			}
		}

		/// <summary>
		/// Returns the features that are supported by this display.
		/// </summary>
		public eVolumeFeatures SupportedVolumeFeatures
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

				Logger.LogSetTo(eSeverity.Informational, "Volume", m_Volume);

				OnVolumeChanged.Raise(this, new DisplayVolumeApiEventArgs(m_Volume));
			}
		}

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool IsMuted
		{
			get { return m_IsMuted; }
			private set
			{
				try
				{
					if (value == m_IsMuted)
						return;

					m_IsMuted = value;

					Logger.LogSetTo(eSeverity.Informational, "IsMuted", m_IsMuted);

					OnMuteStateChanged.Raise(this, new DisplayMuteApiEventArgs(m_IsMuted));
				}
				finally
				{
					Activities.LogActivity(DisplayActivities.GetMutedActivity(m_IsMuted));
				}
			}
		}

		/// <summary>
		/// The min volume.
		/// </summary>
		public float VolumeDeviceMin { get { return 0; } }

		/// <summary>
		/// The max volume.
		/// </summary>
		public float VolumeDeviceMax { get { return 100; } }

		/// <summary>
		/// Indicates if volume control is currently available or not
		/// </summary>
		public bool VolumeControlAvailable
		{
			get { return m_VolumeControlAvailable; }
			private set
			{
				if (value == m_VolumeControlAvailable)
					return;

				m_VolumeControlAvailable = value;

				OnVolumeControlAvailableChanged.Raise(this, new DisplayVolumeControlAvailableApiEventArgs(VolumeControlAvailable));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public RoomViewConnectedDisplayAdapter()
		{
			// Initialize activities
			IsMuted = false;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnPowerStateChanged = null;
			OnActiveInputChanged = null;
			OnVolumeChanged = null;
			OnMuteStateChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

#if SIMPLSHARP
		/// <summary>
		/// Sets the wrapped display device.
		/// </summary>
		/// <param name="display"></param>
		[PublicAPI]
		public void SetDisplay(RoomViewConnectedDisplay display)
		{
			if (display == m_Display)
				return;

			Unsubscribe(m_Display);

			if (m_Display != null)
				GenericBaseUtils.TearDown(m_Display);

			m_Display = display;

			eDeviceRegistrationUnRegistrationResponse result;
			if (m_Display != null && !GenericBaseUtils.SetUp(m_Display, this, out result))
				Logger.Log(eSeverity.Error, "Unable to register {0} - {1}", m_Display.GetType().Name, result);

			Subscribe(m_Display);
			UpdateCachedOnlineStatus();
		}
#endif

		/// <summary>
		/// Powers the TV.
		/// </summary>
		public void PowerOn()
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			m_Display.PowerOn();
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		public void PowerOff()
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			m_Display.PowerOff();
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		public void SetActiveInput(int address)
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			m_Display.SourceSelectSigs[(uint)address].BoolValue = true;
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Sets the raw volume.
		/// </summary>
		/// <param name="level"></param>
		public void SetVolume(float level)
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			level = MathUtils.Clamp(level, VolumeDeviceMin, VolumeDeviceMax);

			m_Display.Volume.UShortValue = (ushort)MathUtils.MapRange(VolumeDeviceMin, VolumeDeviceMax, 0, ushort.MaxValue, level);
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public void VolumeUpIncrement()
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			m_Display.VolumeUp();
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public void VolumeDownIncrement()
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			m_Display.VolumeDown();
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Mutes the display.
		/// </summary>
		public void MuteOn()
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			m_Display.MuteOn();
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Unmutes the display.
		/// </summary>
		public void MuteOff()
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			m_Display.MuteOff();
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Toggles the mute state of the display.
		/// </summary>
		public void MuteToggle()
		{
#if SIMPLSHARP
			if (m_Display == null)
				throw new InvalidOperationException("Wrapped display is null");

			m_Display.MuteToggle();
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public void VolumeRamp(bool increment, long timeout)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public void VolumeRampStop()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
#if SIMPLSHARP
			return m_Display != null && m_Display.IsOnline;
#else
			return false;
#endif
		}

		private void UpdateCachedVolumeControlAvailableState()
		{
			VolumeControlAvailable = GetVolumeControlAvailable();
		}

		private bool GetVolumeControlAvailable()
		{
#if SIMPLSHARP
			return m_Display != null && m_Display.PowerOnFeedback.BoolValue;
#else
			return false;
#endif
		}

		private void UpdateCachedPowerState()
		{
#if SIMPLSHARP
			if (m_Display == null)
			{
				PowerState = ePowerState.Unknown;
				return;
			}

			if (m_Display.WarmingUpFeedback.BoolValue)
			{
				PowerState = ePowerState.Warming;
				return;
			}

			if (m_Display.CoolingDownFeedback.BoolValue)
			{
				PowerState = ePowerState.Cooling;
				return;
			}

			if (m_Display.PowerOnFeedback.BoolValue)
			{
				PowerState = ePowerState.PowerOn;
				return;
			}

			if (m_Display.PowerOffFeedback.BoolValue)
			{
				PowerState = ePowerState.PowerOff;
				return;
			}

#endif
			PowerState = ePowerState.Unknown;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

#if SIMPLSHARP
			SetDisplay(null);
#endif
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(RoomViewConnectedDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

#if SIMPLSHARP
			settings.Ipid = m_Display == null ? (byte)0 : (byte)m_Display.ID;
#else
			settings.Ipid = 0;
#endif
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(RoomViewConnectedDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

#if SIMPLSHARP
			RoomViewConnectedDisplay display =
				settings.Ipid.HasValue
					? new RoomViewConnectedDisplay(settings.Ipid.Value, ProgramInfo.ControlSystem)
					: null;
			SetDisplay(display);
#else
			throw new NotSupportedException();
#endif
		}

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(RoomViewConnectedDisplaySettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new DisplayRouteDestinationControl(this, 0));
			addControl(new DisplayPowerDeviceControl(this, 1));
			addControl(new DisplayVolumeDeviceControl(this, 2));
		}

		#endregion

#region Display Callbacks

#if !NETSTANDARD
		/// <summary>
		/// Subscribe to the display events.
		/// </summary>
		/// <param name="display"></param>
		private void Subscribe(RoomViewConnectedDisplay display)
		{
			if (display == null)
				return;

			display.OnlineStatusChange += DisplayOnLineStatusChange;
			display.BaseEvent += DisplayOnBaseEvent;
		}

		/// <summary>
		/// Unsubscribe from the display events.
		/// </summary>
		/// <param name="display"></param>
		private void Unsubscribe(RoomViewConnectedDisplay display)
		{
			if (display == null)
				return;

			display.OnlineStatusChange -= DisplayOnLineStatusChange;
			display.BaseEvent -= DisplayOnBaseEvent;
		}

		/// <summary>
		/// Called when a sig value changes.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="args"></param>
		private void DisplayOnBaseEvent(GenericBase device, BaseEventArgs args)
		{
			switch (args.EventId)
			{
				case RoomViewConnectedDisplay.PowerOnFeedbackEventId:
				case RoomViewConnectedDisplay.PowerOffFeedbackEventId:
				case RoomViewConnectedDisplay.PowerStatusFeedbackEventId:
					UpdateCachedPowerState();
					break;

				case RoomViewConnectedDisplay.VolumeUpFeedbackEventId:
				case RoomViewConnectedDisplay.VolumeDownFeedbackEventId:
				case RoomViewConnectedDisplay.VolumeFeedbackEventId:
					Volume = MathUtils.MapRange(0, ushort.MaxValue, VolumeDeviceMin, VolumeDeviceMax,
					                            m_Display.VolumeFeedback.GetUShortValueOrDefault());
					break;

				case RoomViewConnectedDisplay.MuteOnFeedbackEventId:
				case RoomViewConnectedDisplay.MuteOffFeedbackEventId:
					IsMuted = m_Display.MuteOnFeedback.GetBoolValueOrDefault();
					break;

				case RoomViewConnectedDisplay.SourceSelectFeedbackEventId:
				case RoomViewConnectedDisplay.CurrentSourceFeedbackEventId:
					KeyValuePair<uint, BoolOutputSig> active;
					bool any =
						(m_Display.SourceSelectFeedbackSigs as IEnumerable<KeyValuePair<uint, BoolOutputSig>>)
							.TryFirst(kvp => kvp.Value.GetBoolValueOrDefault(), out active);
					ActiveInput = any ? (int?)active.Key : null;
					break;
			}
		}

		/// <summary>
		/// Called when the dispay online status changes.
		/// </summary>
		/// <param name="currentDevice"></param>
		/// <param name="args"></param>
		private void DisplayOnLineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}
#endif

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
