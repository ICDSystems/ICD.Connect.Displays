using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public sealed class SimplDisplayWithAudio : AbstractSimplDisplay<SimplDisplayWithAudioSettings>, ISimplDisplayWithAudio
	{
		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		public event EventHandler<DisplayVolumeApiEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<DisplayMuteApiEventArgs> OnMuteStateChanged;

		private float m_Volume;
		private bool m_IsMuted;

		#region Callbacks

		public SimplDisplayWithAudioSetVolumeCallback SetVolumeCallback { get; set; }

		public SimplDisplayWithAudioVolumeUpIncrementCallback VolumeUpIncrementCallback { get; set; }

		public SimplDisplayWithAudioVolumeDownIncrementCallback VolumeDownIncrementCallback { get; set; }

		public SimplDisplayWithAudioMuteOnCallback MuteOnCallback { get; set; }

		public SimplDisplayWithAudioMuteOffCallback MuteOffCallback { get; set; }

		public SimplDisplayWithAudioMuteToggleCallback MuteToggleCallback { get; set; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the raw volume of the display.
		/// </summary>
		public float Volume
		{
			get { return m_Volume; }
			set
			{
				if (Math.Abs(value - m_Volume) < 0.01f)
					return;

				m_Volume = value;

				Log(eSeverity.Informational, "Raw volume set to {0}", StringUtils.NiceName(m_Volume));

				OnVolumeChanged.Raise(this, new DisplayVolumeApiEventArgs(m_Volume));
			}
		}

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool IsMuted
		{
			get { return m_IsMuted; }
			set
			{
				if (value == m_IsMuted)
					return;

				m_IsMuted = value;

				Log(eSeverity.Informational, "Mute set to {0}", m_IsMuted);

				OnMuteStateChanged.Raise(this, new DisplayMuteApiEventArgs(m_IsMuted));
			}
		}

		/// <summary>
		/// The min volume.
		/// </summary>
		public float VolumeDeviceMin { get; set; }

		/// <summary>
		/// The max volume.
		/// </summary>
		public float VolumeDeviceMax { get; set; }

		/// <summary>
		/// Prevents the device from going below this volume.
		/// </summary>
		public float? VolumeSafetyMin { get; set; }

		/// <summary>
		/// Prevents the device from going above this volume.
		/// </summary>
		public float? VolumeSafetyMax { get; set; }

		/// <summary>
		/// The volume the device is set to when powered.
		/// </summary>
		public float? VolumeDefault { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public SimplDisplayWithAudio()
		{
			VolumeDeviceMin = ushort.MinValue;
			VolumeDeviceMax = ushort.MaxValue;
			Controls.Add(new DisplayVolumeDeviceControl(this, 2));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnVolumeChanged = null;
			OnMuteStateChanged = null;

			SetVolumeCallback = null;
			VolumeUpIncrementCallback = null;
			VolumeDownIncrementCallback = null;
			MuteOnCallback = null;
			MuteOffCallback = null;
			MuteToggleCallback = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Sets the current volume.
		/// </summary>
		/// <param name="raw"></param>
		public void SetVolume(float raw)
		{
			SimplDisplayWithAudioSetVolumeCallback handler = SetVolumeCallback;
			if (handler != null)
				handler(this, raw);
		}

		/// <summary>
		/// Increments the current volume.
		/// </summary>
		public void VolumeUpIncrement()
		{
			SimplDisplayWithAudioVolumeUpIncrementCallback handler = VolumeUpIncrementCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Decrements the current volume.
		/// </summary>
		public void VolumeDownIncrement()
		{
			SimplDisplayWithAudioVolumeDownIncrementCallback handler = VolumeDownIncrementCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Mutes the display.
		/// </summary>
		public void MuteOn()
		{
			SimplDisplayWithAudioMuteOnCallback handler = MuteOnCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Unmutes the display.
		/// </summary>
		public void MuteOff()
		{
			SimplDisplayWithAudioMuteOffCallback handler = MuteOffCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Toggles the current mute state on the display.
		/// </summary>
		public void MuteToggle()
		{
			SimplDisplayWithAudioMuteToggleCallback handler = MuteToggleCallback;
			if (handler != null)
				handler(this);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SimplDisplayWithAudioSettings settings)
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
		protected override void ApplySettingsFinal(SimplDisplayWithAudioSettings settings, IDeviceFactory factory)
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
