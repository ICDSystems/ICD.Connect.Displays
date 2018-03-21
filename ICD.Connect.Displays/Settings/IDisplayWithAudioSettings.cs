namespace ICD.Connect.Displays.Settings
{
	public interface IDisplayWithAudioSettings : IDisplaySettings
	{
		float? VolumeSafetyMin { get; set; }

		float? VolumeSafetyMax { get; set; }

		float? VolumeDefault { get; set; }
	}
}