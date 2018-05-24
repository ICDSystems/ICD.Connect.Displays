using ICD.Connect.Displays.Devices;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public delegate void SimplDisplayWithAudioSetVolumeCallback(ISimplDisplayWithAudio sender, float volume);

	public delegate void SimplDisplayWithAudioVolumeUpIncrementCallback(ISimplDisplayWithAudio sender);

	public delegate void SimplDisplayWithAudioVolumeDownIncrementCallback(ISimplDisplayWithAudio sender);

	public delegate void SimplDisplayWithAudioMuteOnCallback(ISimplDisplayWithAudio sender);

	public delegate void SimplDisplayWithAudioMuteOffCallback(ISimplDisplayWithAudio sender);

	public delegate void SimplDisplayWithAudioMuteToggleCallback(ISimplDisplayWithAudio sender);

	public interface ISimplDisplayWithAudio : ISimplDisplay, IDisplayWithAudio
	{
		SimplDisplayWithAudioSetVolumeCallback SetVolumeCallback { get; set; }

		SimplDisplayWithAudioVolumeUpIncrementCallback VolumeUpIncrementCallback { get; set; }

		SimplDisplayWithAudioVolumeDownIncrementCallback VolumeDownIncrementCallback { get; set; }

		SimplDisplayWithAudioMuteOnCallback MuteOnCallback { get; set; }

		SimplDisplayWithAudioMuteOffCallback MuteOffCallback { get; set; }

		SimplDisplayWithAudioMuteToggleCallback MuteToggleCallback { get; set; }

		/// <summary>
		/// Gets/sets the current volume.
		/// </summary>
		new float Volume { get; set; }

		/// <summary>
		/// Gets/sets the muted state.
		/// </summary>
		new bool IsMuted { get; set; }

		/// <summary>
		/// Gets/sets the min volume.
		/// </summary>
		new float VolumeDeviceMin { get; set; }

		/// <summary>
		/// Gets/sets the max volume.
		/// </summary>
		new float VolumeDeviceMax { get; set; }
	}
}
