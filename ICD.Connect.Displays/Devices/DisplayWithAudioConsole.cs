using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Displays.Devices
{
	public static class DisplayWithAudioConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IDisplayWithAudio instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(IDisplayWithAudio instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			float volume = instance.GetVolumeAsPercentage() * 100;
			string percentage = string.Format("{0}%", (int)volume);

			addRow("Volume Control Available", instance.VolumeControlAvailable);
			addRow("Muted", instance.IsMuted);
			addRow("Volume", instance.Volume);
			addRow("Volume Percentage", percentage);
			addRow("Device volume range", string.Format("{0} - {1}", instance.VolumeDeviceMin, instance.VolumeDeviceMax));
			addRow("Safety volume range", string.Format("{0} - {1}", instance.VolumeSafetyMin, instance.VolumeSafetyMax));
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IDisplayWithAudio instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new ConsoleCommand("MuteOn", "Mutes the audio", () => instance.MuteOn());
			yield return new ConsoleCommand("MuteOff", "Unmutes the audio", () => instance.MuteOff());
			yield return new ConsoleCommand("MuteToggle", "Toggles the audio mute state", () => instance.MuteToggle());

			string setVolumeHelp = string.Format("SetVolume <{0}>",
			                                     StringUtils.RangeFormat(instance.GetVolumeSafetyOrDeviceMin(),
			                                                             instance.GetVolumeSafetyOrDeviceMax()));
			yield return new GenericConsoleCommand<float>("SetVolume", setVolumeHelp, f => instance.SetVolume(f));

			string setSafetyMinVolumeHelp = string.Format("SetSafetyMinVolume <{0}>",
			                                              StringUtils.RangeFormat(instance.VolumeDeviceMin,
			                                                                      instance.VolumeDeviceMax));
			yield return
				new GenericConsoleCommand<float>("SetSafetyMinVolume", setSafetyMinVolumeHelp, v => instance.VolumeSafetyMin = v);
			yield return new ConsoleCommand("ClearSafetyMinVolume", "", () => instance.VolumeSafetyMin = null);

			string setSafetyMaxVolumeHelp = string.Format("SetSafetyMaxVolume <{0}>",
			                                              StringUtils.RangeFormat(instance.VolumeDeviceMin,
			                                                                      instance.VolumeDeviceMax));
			yield return
				new GenericConsoleCommand<float>("SetSafetyMaxVolume", setSafetyMaxVolumeHelp, v => instance.VolumeSafetyMax = v);
			yield return new ConsoleCommand("ClearSafetyMaxVolume", "", () => instance.VolumeSafetyMax = null);
		}
	}
}
