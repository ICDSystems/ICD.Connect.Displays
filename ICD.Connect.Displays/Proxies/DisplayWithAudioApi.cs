namespace ICD.Connect.Displays.Proxies
{
	public static class DisplayWithAudioApi
	{
		public const string EVENT_VOLUME = "OnVolumeChanged";
		public const string EVENT_IS_MUTED = "OnMuteStateChanged";
		public const string EVENT_VOLUME_CONTROL_AVAILABLE = "OnVolumeControlAvailableChanged";

		public const string PROPERTY_SUPPORTED_VOLUME_FEATURES = "SupportedVolumeFeatures";
		public const string PROPERTY_VOLUME = "Volume";
		public const string PROPERTY_IS_MUTED = "IsMuted";
		public const string PROPERTY_VOLUME_DEVICE_MIN = "VolumeDeviceMin";
		public const string PROPERTY_VOLUME_DEVICE_MAX = "VolumeDeviceMax";
		public const string PROPERTY_VOLUME_CONTROL_AVAILABLE = "VolumeControlAvailable";

		public const string METHOD_SET_VOLUME = "SetVolume";
		public const string METHOD_VOLUME_UP_INCREMENT = "VolumeUpIncrement";
		public const string METHOD_VOLUME_DOWN_INCREMENT = "VolumeDownIncrement";
		public const string METHOD_VOLUME_RAMP = "VolumeRamp";
		public const string METHOD_VOLUME_RAMP_STOP = "VolumeRampStop";
		public const string METHOD_MUTE_ON = "MuteOn";
		public const string METHOD_MUTE_OFF = "MuteOff";
		public const string METHOD_MUTE_TOGGLE = "MuteToggle";

		public const string HELP_EVENT_VOLUME = "Raised when the volume changes.";
		public const string HELP_EVENT_IS_MUTED = "Raised when the mute state changes.";
		public const string HELP_EVENT_VOLUME_CONTROL_AVAILABLE = "Raised when the volume control availability changes.";

		public const string HELP_PROPERTY_SUPPORTED_VOLUME_FEATURES = "Returns true if the control will raise feedback for the current mute state.";
		public const string HELP_PROPERTY_VOLUME = "Gets the volume of the display.";
		public const string HELP_PROPERTY_IS_MUTED = "Gets the muted state of the display.";
		public const string HELP_PROPERTY_VOLUME_DEVICE_MIN = "Gets the min volume of the display.";
		public const string HELP_PROPERTY_VOLUME_DEVICE_MAX = "Gets the max volume of the display.";
		public const string HELP_PROPERTY_VOLUME_CONTROL_AVAILABLE = "Gets the availability of the volume control";

		public const string HELP_METHOD_SET_VOLUME = "Sets the display volume.";
		public const string HELP_METHOD_VOLUME_UP_INCREMENT = "Increments the display volume.";
		public const string HELP_METHOD_VOLUME_DOWN_INCREMENT = "Decrements the display volume.";
		public const string HELP_METHOD_VOLUME_RAMP = "Starts ramping the volume, and continues until stop is called or the timeout is reached.";
		public const string HELP_METHOD_VOLUME_RAMP_STOP = "Stops any current ramp up/down in progress.";
		public const string HELP_METHOD_MUTE_ON = "Mutes the display.";
		public const string HELP_METHOD_MUTE_OFF = "Unmutes the display.";
		public const string HELP_METHOD_MUTE_TOGGLE = "Toggles the display mute state.";
	}
}
