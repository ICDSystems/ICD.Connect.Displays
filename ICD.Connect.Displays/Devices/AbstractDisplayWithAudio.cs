using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Settings;

namespace ICD.Connect.Displays.Devices
{
	public abstract class AbstractDisplayWithAudio<T> : AbstractDisplay<T>, IDisplayWithAudio
		where T : IDisplayWithAudioSettings, new()
	{
		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<DisplayMuteApiEventArgs> OnMuteStateChanged;

		public event EventHandler<DisplayVolumeControlAvailableApiEventArgs> OnVolumeControlAvailableChanged;

		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		public event EventHandler<DisplayVolumeApiEventArgs> OnVolumeChanged;

		private bool m_IsMuted;
		private float m_Volume;

		private bool m_VolumeControlAvailable;

		#region Properties

		/// <summary>
		/// Returns the features that are supported by this display.
		/// </summary>
		public abstract eVolumeFeatures SupportedVolumeFeatures { get; }

		/// <summary>
		/// Override if the display volume minimum is not 0.
		/// </summary>
		public virtual float VolumeDeviceMin { get { return 0; } }

		/// <summary>
		/// Override if the display volume maximum is not 100.
		/// </summary>
		public virtual float VolumeDeviceMax { get { return 100; } }

		/// <summary>
		/// Gets the raw volume of the display.
		/// </summary>
		public float Volume
		{
			get { return m_Volume; }
			protected set
			{
				if (Math.Abs(value - m_Volume) < 0.01f)
					return;

				m_Volume = value;

				Log(eSeverity.Informational, "Raw volume set to {0:F2}", m_Volume);

				OnVolumeChanged.Raise(this, new DisplayVolumeApiEventArgs(m_Volume));
			}
		}

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool IsMuted
		{
			get { return m_IsMuted; }
			protected set
			{
				if (value == m_IsMuted)
					return;

				m_IsMuted = value;

				Log(eSeverity.Informational, "Mute set to {0}", m_IsMuted);

				OnMuteStateChanged.Raise(this, new DisplayMuteApiEventArgs(m_IsMuted));
			}
		}

		/// <summary>
		/// Indicates if volume control is currently available or not
		/// </summary>
		public bool VolumeControlAvailable
		{
			get { return m_VolumeControlAvailable; }
			private set
			{
				if (m_VolumeControlAvailable == value)
					return;

				m_VolumeControlAvailable = value;

				OnVolumeControlAvailableChanged.Raise(this, new DisplayVolumeControlAvailableApiEventArgs(VolumeControlAvailable));
			}
		}

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		public override ePowerState PowerState
		{
			get { return base.PowerState; }
			protected set
			{
				if (value == PowerState)
					return;

				base.PowerState = value;

				UpdateCachedVolumeControlAvailableState();
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractDisplayWithAudio()
		{
			Controls.Add(new DisplayVolumeDeviceControl(this, 2));
		}

		/// <summary>
		/// Clears resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnMuteStateChanged = null;
			OnVolumeChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Enables mute.
		/// </summary>
		public abstract void MuteOn();

		/// <summary>
		/// Disables mute.
		/// </summary>
		public abstract void MuteOff();

		/// <summary>
		/// Toggles mute.
		/// </summary>
		public virtual void MuteToggle()
		{
			if (IsMuted)
				MuteOff();
			else
				MuteOn();
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public abstract void VolumeRamp(bool increment, long timeout);

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public abstract void VolumeRampStop();

		/// <summary>
		/// Sets the raw volume.
		/// </summary>
		/// <param name="level"></param>
		public void SetVolume(float level)
		{
			if (PowerState != ePowerState.PowerOn && PowerState != ePowerState.Warming)
				return;

			level = MathUtils.Clamp(level, VolumeDeviceMin, VolumeDeviceMax);

			SetVolumeFinal(level);
		}

		/// <summary>
		/// Increments the raw volume.
		/// </summary>
		public abstract void VolumeUpIncrement();

		/// <summary>
		/// Decrements the raw volume.
		/// </summary>
		public abstract void VolumeDownIncrement();

		#endregion

		#region Private Method

		/// <summary>
		/// Sends the volume set command to the device after validation has been performed.
		/// </summary>
		/// <param name="raw"></param>
		protected abstract void SetVolumeFinal(float raw);

		protected virtual bool GetVolumeControlAvailable()
		{
			return PowerState == ePowerState.PowerOn;
		}

		protected virtual void UpdateCachedVolumeControlAvailableState()
		{
			VolumeControlAvailable = GetVolumeControlAvailable();
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
