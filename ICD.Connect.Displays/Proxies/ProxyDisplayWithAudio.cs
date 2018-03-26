using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.Devices;

namespace ICD.Connect.Displays.Proxies
{
	public sealed class ProxyDisplayWithAudio : AbstractProxyDisplay, IProxyDisplayWithAudio
	{
		public event EventHandler<FloatEventArgs> OnVolumeChanged;

		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		[ApiProperty("Volume", "Gets the volume of the display.")]
		public float Volume { get; private set; }

		[ApiProperty("IsMuted", "Gets the muted state of the display.")]
		public bool IsMuted { get; private set; }

		[ApiProperty("VolumeDeviceMin", "Gets the min volume of the display.")]
		public float VolumeDeviceMin { get; private set; }

		[ApiProperty("VolumeDeviceMax", "Gets the max volume of the display.")]
		public float VolumeDeviceMax { get; private set; }

		[ApiProperty("VolumeSafetyMin", "Gets/sets the min safety volume.")]
		public float? VolumeSafetyMin { get; set; }

		[ApiProperty("VolumeSafetyMax", "Gets/sets the max safety volume.")]
		public float? VolumeSafetyMax { get; set; }

		[ApiProperty("VolumeDefault", "Gets/sets the default volume.")]
		public float? VolumeDefault { get; set; }

		[ApiMethod("SetVolume", "Sets the display volume.")]
		public void SetVolume(float raw)
		{
			throw new NotImplementedException();
		}

		[ApiMethod("VolumeUpIncrement", "Increments the display volume.")]
		public void VolumeUpIncrement()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("VolumeDownIncrement", "Decrements the display volume.")]
		public void VolumeDownIncrement()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("MuteOn", "Mutes the display.")]
		public void MuteOn()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("MuteOff", "Unmutes the display.")]
		public void MuteOff()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("MuteToggle", "Toggles the display mute state.")]
		public void MuteToggle()
		{
			throw new NotImplementedException();
		}

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
