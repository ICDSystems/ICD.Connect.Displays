namespace ICD.Connect.Displays.Proxies
{
	public static class DisplayApi
	{
		public const string EVENT_IS_POWERED = "OnIsPoweredChanged";
		public const string EVENT_ACTIVE_INPUT = "OnActiveInputChanged";
		public const string EVENT_SCALING_MODE = "OnScalingModeChanged";

		public const string PROPERTY_TRUST = "Trust";
		public const string PROPERTY_IS_POWERED = "IsPowered";
		public const string PROPERTY_ACTIVE_INPUT = "ActiveInput";
		public const string PROPERTY_SCALING_MODE = "ScalingMode";

		public const string METHOD_POWER_ON = "PowerOn";
		public const string METHOD_POWER_OFF = "PowerOff";
		public const string METHOD_SET_ACTIVE_INPUT = "SetActiveInput";
		public const string METHOD_SET_SCALING_MODE = "SetScalingMode";

		public const string HELP_EVENT_IS_POWERED = "Raised when the power state changes.";
		public const string HELP_EVENT_ACTIVE_INPUT = "Raised when the active input changes.";
		public const string HELP_EVENT_SCALING_MODE = "Raised when the scaling mode changes.";

		public const string HELP_PROPERTY_TRUST = "When true assume TX is successful even if a request times out.";
		public const string HELP_PROPERTY_IS_POWERED = "Gets the powered state for the display.";
		public const string HELP_PROPERTY_ACTIVE_INPUT = "Gets the active input for the display.";
		public const string HELP_PROPERTY_SCALING_MODE = "Gets the scaling mode for the display.";

		public const string HELP_METHOD_POWER_ON = "Powers the display.";
		public const string HELP_METHOD_POWER_OFF = "Powers off the display.";
		public const string HELP_METHOD_SET_ACTIVE_INPUT = "Sets the active input for the display.";
		public const string HELP_METHOD_SET_SCALING_MODE = "Sets the scaling mode for the display.";
	}
}
