namespace ICD.Connect.Displays.Sharp
{
	public static class SharpDisplayCommands
	{
		public const string RETURN = "\x0D";

		public const string OK = "OK" + RETURN;
		public const string ERROR = "ERR" + RETURN;

		public const string POWER = "POWR";
		public const string MUTE = "MUTE";
		public const string VOLUME = "VOLM";
		public const string INPUT = "IAVD";
		public const string WIDE = "WIDE";
		public const string REMOTE_CONTROL_BUTTONS = "RCKY";
		public const string QUERY = "????";

		public const string POWER_ON = POWER + "1   " + RETURN;
		public const string POWER_OFF = POWER + "0   " + RETURN;
		public const string POWER_ON_COMMAND = "RSPW1   " + RETURN;
		public const string POWER_QUERY = POWER + QUERY + RETURN;

		public const string MUTE_TOGGLE = MUTE + "0   " + RETURN;
		public const string MUTE_ON = MUTE + "1   " + RETURN;
		public const string MUTE_OFF = MUTE + "2   " + RETURN;
		public const string MUTE_QUERY = MUTE + QUERY + RETURN;

		public const string INPUT_HDMI_1 = INPUT + "1   " + RETURN;
		public const string INPUT_HDMI_2 = INPUT + "2   " + RETURN;
		public const string INPUT_HDMI_3 = INPUT + "3   " + RETURN;
		public const string INPUT_HDMI_4 = INPUT + "4   " + RETURN;
		public const string INPUT_HDMI_QUERY = INPUT + QUERY + RETURN;

		public const string VOLUME_DOWN = REMOTE_CONTROL_BUTTONS + "32  " + RETURN;
		public const string VOLUME_UP = REMOTE_CONTROL_BUTTONS + "33  " + RETURN;
		public const string VOLUME_QUERY = VOLUME + QUERY + RETURN;

		public const string SCALING_MODE_16_X9 = WIDE + "40  " + RETURN; // Stretch
		public const string SCALING_MODE_4_X3 = WIDE + "20  " + RETURN; // S. Stretch
		public const string SCALING_MODE_NO_SCALE = WIDE + "80  " + RETURN; // Dot by dot
		public const string SCALING_MODE_ZOOM = WIDE + "30  " + RETURN; // Zoom AV
		public const string SCALING_MODE_QUERY = WIDE + QUERY + RETURN;

		/// <summary>
		/// Builds the string for the command.
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string GetCommand(string prefix, string parameters)
		{
			return prefix + parameters.PadRight(4, ' ') + RETURN;
		}
	}
}