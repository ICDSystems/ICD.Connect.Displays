namespace ICD.Connect.Displays.Proxies
{
	public static class DisplayWithAudioApi
	{
		public const string PROPERTY_VOLUME = "Volume";
		public const string PROPERTY_IS_MUTED = "IsMuted";
		public const string PROPERTY_VOLUME_DEVICE_MIN = "VolumeDeviceMin";
		public const string PROPERTY_VOLUME_DEVICE_MAX = "VolumeDeviceMax";
		public const string PROPERTY_VOLUME_SAFETY_MIN = "VolumeSafetyMin";
		public const string PROPERTY_VOLUME_SAFETY_MAX = "VolumeSafetyMax";
		public const string PROPERTY_VOLUME_DEFAULT = "VolumeDefault";

		public const string METHOD_SET_VOLUME = "SetVolume";
		public const string METHOD_VOLUME_UP_INCREMENT = "VolumeUpIncrement";
		public const string METHOD_VOLUME_DOWN_INCREMENT = "VolumeDownIncrement";
		public const string METHOD_MUTE_ON = "MuteOn";
		public const string METHOD_MUTE_OFF = "MuteOff";
		public const string METHOD_MUTE_TOGGLE = "MuteToggle";

		public const string HELP_PROPERTY_VOLUME = "Gets the volume of the display.";
		public const string HELP_PROPERTY_IS_MUTED = "Gets the muted state of the display.";
		public const string HELP_PROPERTY_VOLUME_DEVICE_MIN = "Gets the min volume of the display.";
		public const string HELP_PROPERTY_VOLUME_DEVICE_MAX = "Gets the max volume of the display.";
		public const string HELP_PROPERTY_VOLUME_SAFETY_MIN = "Gets/sets the min safety volume.";
		public const string HELP_PROPERTY_VOLUME_SAFETY_MAX = "Gets/sets the max safety volume.";
		public const string HELP_PROPERTY_VOLUME_DEFAULT = "Gets/sets the default volume.";

		public const string HELP_METHOD_SET_VOLUME = "Sets the display volume.";
		public const string HELP_METHOD_VOLUME_UP_INCREMENT = "Increments the display volume.";
		public const string HELP_METHOD_VOLUME_DOWN_INCREMENT = "Decrements the display volume.";
		public const string HELP_METHOD_MUTE_ON = "Mutes the display.";
		public const string HELP_METHOD_MUTE_OFF = "Unmutes the display.";
		public const string HELP_METHOD_MUTE_TOGGLE = "Toggles the display mute state.";
	}
}
