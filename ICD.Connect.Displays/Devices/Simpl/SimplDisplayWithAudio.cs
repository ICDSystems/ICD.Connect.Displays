using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Displays.Devices.Simpl
{
	public delegate void SimplDisplayWithAudioSetVolumeCallback(IDisplayWithAudio sender, float volume);

	public delegate void SimplDisplayWithAudioVolumeUpIncrementCallback(IDisplayWithAudio sender);

	public delegate void SimplDisplayWithAudioVolumeDownIncrementCallback(IDisplayWithAudio sender);

	public delegate void SimplDisplayWithAudioMuteOnCallback(IDisplayWithAudio sender);

	public delegate void SimplDisplayWithAudioMuteOffCallback(IDisplayWithAudio sender);

	public delegate void SimplDisplayWithAudioMuteToggleCallback(IDisplayWithAudio sender);

	public sealed class SimplDisplayWithAudio : AbstractSimplDisplay<SimplDisplayWithAudioSettings>, IDisplayWithAudio
	{
		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		public event EventHandler<FloatEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

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

				Logger.AddEntry(eSeverity.Informational, "{0} - Raw volume set to {1}", StringUtils.NiceName(m_Volume));

				OnVolumeChanged.Raise(this, new FloatEventArgs(m_Volume));
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

				Logger.AddEntry(eSeverity.Informational, "{0} - Mute set to {1}", m_IsMuted);

				OnMuteStateChanged.Raise(this, new BoolEventArgs(m_IsMuted));
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
			Controls.Add(new DisplayVolumeDeviceControl(this, 1));
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
