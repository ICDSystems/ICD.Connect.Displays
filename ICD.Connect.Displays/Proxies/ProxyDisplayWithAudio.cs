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
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Proxies
{
	public sealed class ProxyDisplayWithAudio : AbstractProxyDisplay, IProxyDisplayWithAudio
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

		#region Properties

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
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_IS_MUTED)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME_DEFAULT)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MAX)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MIN)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME_SAFETY_MAX)
			                 .GetProperty(DisplayWithAudioApi.PROPERTY_VOLUME_SAFETY_MIN)
							 .Complete();
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
				case DisplayWithAudioApi.PROPERTY_VOLUME_DEFAULT:
					VolumeDefault = result.GetValue<float?>();
					break;
				case DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MAX:
					VolumeDeviceMax = result.GetValue<float>();
					break;
				case DisplayWithAudioApi.PROPERTY_VOLUME_DEVICE_MIN:
					VolumeDeviceMin = result.GetValue<float>();
					break;
				case DisplayWithAudioApi.PROPERTY_VOLUME_SAFETY_MAX:
					VolumeSafetyMax = result.GetValue<float?>();
					break;
				case DisplayWithAudioApi.PROPERTY_VOLUME_SAFETY_MIN:
					VolumeSafetyMin = result.GetValue<float?>();
					break;
			}
		}

		#region Methods

		/// <summary>
		/// Sets the raw volume.
		/// </summary>
		/// <param name="raw"></param>
		public void SetVolume(float raw)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Increments the volume once.
		/// </summary>
		public void VolumeUpIncrement()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Decrements the volume once.
		/// </summary>
		public void VolumeDownIncrement()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Mutes the display.
		/// </summary>
		public void MuteOn()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Unmutes the display.
		/// </summary>
		public void MuteOff()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Toggles the mute state of the display.
		/// </summary>
		public void MuteToggle()
		{
			throw new NotImplementedException();
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
