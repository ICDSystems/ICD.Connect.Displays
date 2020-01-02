using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Info;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Proxies
{
	public sealed class ProxyDisplayWithAudio : AbstractProxyDisplay<ProxyDisplayWithAudioSettings>, IProxyDisplayWithAudio
	{
		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		public event EventHandler<DisplayVolumeApiEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<DisplayMuteApiEventArgs> OnMuteStateChanged;

		public event EventHandler<DisplayVolumeControlAvailableApiEventArgs> OnVolumeControlAvailableChanged;

		private float m_Volume;
		private bool m_IsMuted;
		private bool m_VolumeControlAvailable;

		#region Properties

		/// <summary>
		/// Returns the features that are supported by this display.
		/// </summary>
		public eVolumeFeatures SupportedVolumeFeatures { get; [UsedImplicitly] private set; }

		/// <summary>
		/// Gets the current volume.
		/// </summary>
		public float Volume
		{
			get { return m_Volume; }
			[UsedImplicitly] private set
			{
				if (Math.Abs(value - m_Volume) < 0.01f)
					return;

				m_Volume = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - Raw volume set to {1}", this, StringUtils.NiceName(m_Volume));

				OnVolumeChanged.Raise(this, new DisplayVolumeApiEventArgs(m_Volume));
			}
		}

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool IsMuted
		{
			get { return m_IsMuted; }
			[UsedImplicitly] private set
			{
				if (value == m_IsMuted)
					return;

				m_IsMuted = value;

				Logger.AddEntry(eSeverity.Informational, "{0} - Mute set to {1}", this, m_IsMuted);

				OnMuteStateChanged.Raise(this, new DisplayMuteApiEventArgs(m_IsMuted));
			}
		}

		/// <summary>
		/// The min volume.
		/// </summary>
		public float VolumeDeviceMin { get; [UsedImplicitly] private set; }

		/// <summary>
		/// The max volume.
		/// </summary>
		public float VolumeDeviceMax { get; [UsedImplicitly] private set; }

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

		#endregion

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnVolumeChanged = null;
			OnMuteStateChanged = null;

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Override to build initialization commands on top of the current class info.
		/// </summary>
		/// <param name="command"></param>
		protected override void Initialize(ApiClassInfo command)
		{
			base.Initialize(command);

			ApiCommandBuilder.UpdateCommand(command)
			                 .SubscribeEvent(DisplayWithAudioApi.EVENT_VOLUME)
			                 .SubscribeEvent(DisplayWithAudioApi.EVENT_IS_MUTED)
			                 .SubscribeEvent(DisplayWithAudioApi.EVENT_VOLUME_CONTROL_AVAILABLE)
							 .GetProperty(DisplayWithAudioApi.PROPERTY_SUPPORTED_VOLUME_FEATURES)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_IS_MUTED)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MAX)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MIN)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME_CONTROL_AVAILABLE)
							 .Complete();
		}

		/// <summary>
		/// Updates the proxy with event feedback info.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		protected override void ParseEvent(string name, ApiResult result)
		{
			base.ParseEvent(name, result);

			switch (name)
			{
				case DisplayWithAudioApi.EVENT_VOLUME:
					Volume = result.GetValue<float>();
					break;
				case DisplayWithAudioApi.EVENT_IS_MUTED:
					IsMuted = result.GetValue<bool>();
					break;
				case DisplayWithAudioApi.EVENT_VOLUME_CONTROL_AVAILABLE:
					VolumeControlAvailable = result.GetValue<bool>();
					break;
			}
		}

		/// <summary>
		/// Updates the proxy with a property result.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		protected override void ParseProperty(string name, ApiResult result)
		{
			base.ParseProperty(name, result);

			switch (name)
			{
				case DisplayWithAudioApi.PROPERTY_VOLUME:
					Volume = result.GetValue<float>();
					break;
				case DisplayWithAudioApi.PROPERTY_IS_MUTED:
					IsMuted = result.GetValue<bool>();
					break;
				case DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MAX:
					VolumeDeviceMax = result.GetValue<float>();
					break;
				case DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MIN:
					VolumeDeviceMin = result.GetValue<float>();
					break;
				case DisplayWithAudioApi.PROPERTY_VOLUME_CONTROL_AVAILABLE:
					VolumeControlAvailable = result.GetValue<bool>();
					break;
			}
		}

		#region Methods

		/// <summary>
		/// Sets the raw volume.
		/// </summary>
		/// <param name="level"></param>
		public void SetVolume(float level)
		{
			CallMethod(DisplayWithAudioApi.METHOD_SET_VOLUME, level);
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public void VolumeUpIncrement()
		{
			CallMethod(DisplayWithAudioApi.METHOD_VOLUME_UP_INCREMENT);
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public void VolumeDownIncrement()
		{
			CallMethod(DisplayWithAudioApi.METHOD_VOLUME_DOWN_INCREMENT);
		}

		/// <summary>
		/// Mutes the display.
		/// </summary>
		public void MuteOn()
		{
			CallMethod(DisplayWithAudioApi.METHOD_MUTE_ON);
		}

		/// <summary>
		/// Unmutes the display.
		/// </summary>
		public void MuteOff()
		{
			CallMethod(DisplayWithAudioApi.METHOD_MUTE_OFF);
		}

		/// <summary>
		/// Toggles the mute state of the display.
		/// </summary>
		public void MuteToggle()
		{
			CallMethod(DisplayWithAudioApi.METHOD_MUTE_TOGGLE);
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public void VolumeRamp(bool increment, long timeout)
		{
			CallMethod(DisplayWithAudioApi.METHOD_VOLUME_RAMP, increment, timeout);
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public void VolumeRampStop()
		{
			CallMethod(DisplayWithAudioApi.METHOD_VOLUME_RAMP_STOP);
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
