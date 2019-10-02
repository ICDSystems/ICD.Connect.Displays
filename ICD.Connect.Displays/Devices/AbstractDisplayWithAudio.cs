﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings;

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

		private float? m_VolumeSafetyMin;
		private float? m_VolumeSafetyMax;
		private float? m_VolumeDefault;
		private bool m_VolumeControlAvailable;

		#region Properties

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
			private set
			{
				if (m_VolumeControlAvailable == value)
					return;

				m_VolumeControlAvailable = value;

				OnVolumeControlAvailableChanged.Raise(this, new DisplayVolumeControlAvailableApiEventArgs(VolumeControlAvailable));

				if (VolumeControlAvailable && VolumeDefault != null)
					SetVolume((float)VolumeDefault);
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
		/// Sets the raw volume.
		/// </summary>
		/// <param name="raw"></param>
		public void SetVolume(float raw)
		{
			if (PowerState != ePowerState.PowerOn && PowerState != ePowerState.Warming)
				return;

			// Set the volume
			float min = this.GetVolumeSafetyOrDeviceMin();
			float max = this.GetVolumeSafetyOrDeviceMax();

			raw = MathUtils.Clamp(raw, min, max);
			VolumeSetRawFinal(raw);

			// Update the volume percentage
			min = this.GetVolumeSafetyOrDeviceMin();
			max = this.GetVolumeSafetyOrDeviceMax();

			VolumePercent = Math.Abs(min - max) < 0.01f ? 1.0f : MathUtils.MapRange(min, max,  0.0f, 1.0f, raw);
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
		protected abstract void VolumeSetRawFinal(float raw);

		protected virtual bool GetVolumeControlAvailable()
		{
			return PowerState == ePowerState.PowerOn;
		}

		protected virtual void UpdateCachedVolumeControlAvailableState()
		{
			VolumeControlAvailable = GetVolumeControlAvailable();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(T settings)
		{
			base.CopySettingsFinal(settings);

			settings.VolumeDefault = VolumeDefault;
			settings.VolumeSafetyMin = VolumeSafetyMin;
			settings.VolumeSafetyMax = VolumeSafetyMax;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			VolumeSafetyMin = null;
			VolumeSafetyMax = null;
			VolumeDefault = null;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			VolumeSafetyMin = settings.VolumeSafetyMin;
			VolumeSafetyMax = settings.VolumeSafetyMax;
			VolumeDefault = settings.VolumeDefault;
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
