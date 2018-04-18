using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Devices
{
	public abstract class AbstractDisplayWithAudio<T> : AbstractDisplay<T>, IDisplayWithAudio
		where T : IDisplayWithAudioSettings, new()
	{
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;
		public event EventHandler<FloatEventArgs> OnVolumeChanged;

		private bool m_IsMuted;
		private float m_Volume;

		private float? m_VolumeSafetyMin;
		private float? m_VolumeSafetyMax;
		private float? m_VolumeDefault;

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

				Log(eSeverity.Informational, "Raw volume set to {0}", StringUtils.NiceName(m_Volume));

				// If the volume went outside of safe limits clamp the volume to a safe value.
				float safeVolume = MathUtils.Clamp(m_Volume, this.GetVolumeSafetyOrDeviceMin(), this.GetVolumeSafetyOrDeviceMax());
				if (Math.Abs(m_Volume - safeVolume) > 0.01f)
					SetVolume(safeVolume);

				OnVolumeChanged.Raise(this, new FloatEventArgs(m_Volume));
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

				OnMuteStateChanged.Raise(this, new BoolEventArgs(m_IsMuted));
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
		[PublicAPI]
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
		/// Gets the powered state.
		/// </summary>
		public override sealed bool IsPowered
		{
			get { return base.IsPowered; }
			protected set
			{
				if (value == IsPowered)
					return;

				base.IsPowered = value;

				if (IsPowered && VolumeDefault != null)
					SetVolume((float)VolumeDefault);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractDisplayWithAudio()
		{
			Controls.Add(new DisplayVolumeDeviceControl(this, 1));
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
			if (!IsPowered)
				return;
			raw = MathUtils.Clamp(raw, this.GetVolumeSafetyOrDeviceMin(), this.GetVolumeSafetyOrDeviceMax());
			VolumeSetRawFinal(raw);
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
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			float volume = this.GetVolumeAsPercentage() * 100;
			string percentage = string.Format("{0}%", (int)volume);

			addRow("Muted", IsMuted);
			addRow("Volume", Volume);
			addRow("Volume Percentage", percentage);
			addRow("Device volume range", string.Format("{0} - {1}", VolumeDeviceMin, VolumeDeviceMax));
			addRow("Safety volume range", string.Format("{0} - {1}", VolumeSafetyMin, VolumeSafetyMax));
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("MuteOn", "Mutes the audio", () => MuteOn());
			yield return new ConsoleCommand("MuteOff", "Unmutes the audio", () => MuteOff());
			yield return new ConsoleCommand("MuteToggle", "Toggles the audio mute state", () => MuteToggle());

			string setVolumeHelp = string.Format("SetVolume <{0}>",
			                                     StringUtils.RangeFormat(this.GetVolumeSafetyOrDeviceMin(),
			                                                             this.GetVolumeSafetyOrDeviceMax()));
			yield return new GenericConsoleCommand<float>("SetVolume", setVolumeHelp, f => SetVolume(f));

			string setSafetyMinVolumeHelp = string.Format("SetSafetyMinVolume <{0}>",
			                                              StringUtils.RangeFormat(VolumeDeviceMin, VolumeDeviceMax));
			yield return new GenericConsoleCommand<float>("SetSafetyMinVolume", setSafetyMinVolumeHelp, v => VolumeSafetyMin = v);
			yield return new ConsoleCommand("ClearSafetyMinVolume", "", () => VolumeSafetyMin = null);

			string setSafetyMaxVolumeHelp = string.Format("SetSafetyMaxVolume <{0}>",
			                                              StringUtils.RangeFormat(VolumeDeviceMin, VolumeDeviceMax));
			yield return new GenericConsoleCommand<float>("SetSafetyMaxVolume", setSafetyMaxVolumeHelp, v => VolumeSafetyMax = v);
			yield return new ConsoleCommand("ClearSafetyMaxVolume", "", () => VolumeSafetyMax = null);
		}

		/// <summary>
		/// Workaround for the "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
