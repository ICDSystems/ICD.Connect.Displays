namespace ICD.Connect.Displays.Proxies
{
	public static class DisplayApi
	{
		public const string EVENT_IS_POWERED = "OnIsPoweredChanged";
		public const string EVENT_HDMI_INPUT = "OnHdmiInputChanged";
		public const string EVENT_SCALING_MODE = "OnScalingModeChanged";

		public const string PROPERTY_TRUST = "Trust";
		public const string PROPERTY_IS_POWERED = "IsPowered";
		public const string PROPERTY_INPUT_COUNT = "InputCount";
		public const string PROPERTY_HDMI_INPUT = "HdmiInput";
		public const string PROPERTY_SCALING_MODE = "ScalingMode";

		public const string METHOD_POWER_ON = "PowerOn";
		public const string METHOD_POWER_OFF = "PowerOff";
		public const string METHOD_SET_HDMI_INPUT = "SetHdmiInput";
		public const string METHOD_SET_SCALING_MODE = "SetScalingMode";

		public const string HELP_EVENT_IS_POWERED = "Raised when the power state changes.";
		public const string HELP_EVENT_HDMI_INPUT = "Raised when the selected HDMI input changes.";
		public const string HELP_EVENT_SCALING_MODE = "Raised when the scaling mode changes.";

		public const string HELP_PROPERTY_TRUST = "When true assume TX is successful even if a request times out.";
		public const string HELP_PROPERTY_IS_POWERED = "Gets the powered state for the display.";
		public const string HELP_PROPERTY_INPUT_COUNT = "Gets the HDMI input count for the display.";
		public const string HELP_PROPERTY_HDMI_INPUT = "Gets the current HDMI input for the display.";
		public const string HELP_PROPERTY_SCALING_MODE = "Gets the scaling mode for the display.";

		public const string HELP_METHOD_POWER_ON = "Powers the display.";
		public const string HELP_METHOD_POWER_OFF = "Powers off the display.";
		public const string HELP_METHOD_SET_HDMI_INPUT = "Sets the HDMI input for the display.";
		public const string HELP_METHOD_SET_SCALING_MODE = "Sets the scaling mode for the display.";
	}
}
