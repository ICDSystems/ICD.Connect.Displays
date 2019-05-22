using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.SPlus.EventArgs;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public sealed class SimplDisplayWithAudio : AbstractSimplDisplay<SimplDisplayWithAudioSettings>, ISimplDisplayWithAudio, IDisplayWithAudio
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

		public event EventHandler<SetVolumeApiEventArgs> OnSetVolume;
		public event EventHandler<SetVolumeIncrementApiEventArgs> OnSetVolumeIncrement;
		public event EventHandler<SetMuteApiEventArgs> OnSetMute;
		public event EventHandler<SetMuteToggleApiEventArgs> OnSetMuteToggle;

		public void SetVolumeFeedback(float volume)
		{
			Volume = volume;
		}

		public void SetMuteFeedback(bool mute)
		{
			IsMuted = mute;
		}

		#endregion

		#region Properties

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

				Log(eSeverity.Informational, "Raw volume set to {0}", StringUtils.NiceName(m_Volume));

				OnVolumeChanged.Raise(this, new DisplayVolumeApiEventArgs(m_Volume));
			}
		}

		public float VolumePercent{get { return Volume; }}

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

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Sets the current volume.
		/// </summary>
		/// <param name="raw"></param>
		public void SetVolume(float raw)
		{
			OnSetVolume.Raise(this, new SetVolumeApiEventArgs(raw));

			if (Trust)
				Volume = raw;
		}

		/// <summary>
		/// Increments the current volume.
		/// </summary>
		public void VolumeUpIncrement()
		{

			OnSetVolumeIncrement.Raise(this, new SetVolumeIncrementApiEventArgs(true));

		}

		/// <summary>
		/// Decrements the current volume.
		/// </summary>
		public void VolumeDownIncrement()
		{
			OnSetVolumeIncrement.Raise(this, new SetVolumeIncrementApiEventArgs(false));
		}

		/// <summary>
		/// Mutes the display.
		/// </summary>
		public void MuteOn()
		{
			OnSetMute.Raise(this, new SetMuteApiEventArgs(true));

			if (Trust)
				IsMuted = true;
		}

		/// <summary>
		/// Unmutes the display.
		/// </summary>
		public void MuteOff()
		{
			OnSetMute.Raise(this, new SetMuteApiEventArgs(false));

			if (Trust)
				IsMuted = false;
		}

		/// <summary>
		/// Toggles the current mute state on the display.
		/// </summary>
		public void MuteToggle()
		{
			OnSetMuteToggle.Raise(this, new SetMuteToggleApiEventArgs());

			if (Trust)
				IsMuted = !IsMuted;
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
