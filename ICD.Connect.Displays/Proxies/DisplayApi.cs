﻿namespace ICD.Connect.Displays.Proxies
{
    public static class DisplayApi
    {
	    public const string PROPERTY_IS_POWERED = "IsPowered";
	    public const string PROPERTY_INPUT_COUNT = "InputCount";
	    public const string PROPERTY_HDMI_INPUT = "HdmiInput";
	    public const string PROPERTY_SCALING_MODE = "ScalingMode";

	    public const string METHOD_POWER_ON = "PowerOn";
	    public const string METHOD_POWER_OFF = "PowerOff";
	    public const string METHOD_SET_HDMI_INPUT = "SetHdmiInput";
	    public const string METHOD_SET_SCALING_MODE = "SetScalingMode";

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