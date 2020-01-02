namespace ICD.Connect.Displays
{
	public static class DisplayTelemetryNames
	{
		public const string ACTIVE_INPUT_STATE = "ActiveInput";
		public const string SET_ACTIVE_INPUT = "SetActiveInput";
		public const string ACTIVE_INPUT_STATE_CHANGED = "OnActiveInputChanged";

		public const string SCALING_MODE_STATE = "ScalingMode";
		public const string SET_SCALING_MODE = "SetScalingMode";
		public const string SCALING_MODE_STATE_CHANGED = "OnScalingModeChanged";

		public const string MUTE_STATE = "IsMuted";
		public const string MUTE_STATE_CHANGED = "OnIsMutedChanged";
		public const string MUTE_ON = "MuteOn";
		public const string MUTE_OFF = "MuteOff";

		public const string VOLUME_PERCENT = "Volume";
		public const string VOLUME_PERCENT_CHANGED = "OnVolumeChanged";
		public const string SET_VOLUME = "SetVolume";
	}
}