namespace ICD.Connect.Displays.Proxies
{
	public static class DisplayApi
	{
		public const string EVENT_POWER_STATE = "OnPowerStateChanged";
		public const string EVENT_ACTIVE_INPUT = "OnActiveInputChanged";

		public const string PROPERTY_TRUST = "Trust";
		public const string PROPERTY_POWER_STATE = "PowerState";
		public const string PROPERTY_ACTIVE_INPUT = "ActiveInput";

		public const string METHOD_POWER_ON = "PowerOn";
		public const string METHOD_POWER_OFF = "PowerOff";
		public const string METHOD_SET_ACTIVE_INPUT = "SetActiveInput";

		public const string HELP_EVENT_POWER_STATE = "Raised when the power state changes.";
		public const string HELP_EVENT_ACTIVE_INPUT = "Raised when the active input changes.";

		public const string HELP_PROPERTY_TRUST = "When true assume TX is successful even if a request times out.";
		public const string HELP_PROPERTY_POWER_STATE = "Gets the powered state for the display.";
		public const string HELP_PROPERTY_ACTIVE_INPUT = "Gets the active input for the display.";

		public const string HELP_METHOD_POWER_ON = "Powers the display.";
		public const string HELP_METHOD_POWER_OFF = "Powers off the display.";
		public const string HELP_METHOD_SET_ACTIVE_INPUT = "Sets the active input for the display.";
	}
}
