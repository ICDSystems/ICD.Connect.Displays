using System;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	public static class SamsungCommand
	{
		public const int MAX_RETRIES = 50;

		public const string RETURN = "\x03\x0C";

		public const string SUCCESS = RETURN + "\xF1";
		public const string FAILURE = RETURN + "\xFF";

		public const string COMMAND_PREFIX = "\x08\x22";

		public const string POWER_PREFIX = COMMAND_PREFIX + "\x00\x00\x00";
		public const string POWER_ON = POWER_PREFIX + "\x02";
		public const string POWER_OFF = POWER_PREFIX + "\x01";
		public const string POWER_TOGGLE = POWER_PREFIX + "\x00";

		public const string MUTE_PREFIX = COMMAND_PREFIX + "\x02\x00\x00";
		public const string MUTE_TOGGLE = MUTE_PREFIX + "\x00";
		public const string MUTE_ON = MUTE_PREFIX + "\x01";
		public const string MUTE_OFF = MUTE_PREFIX + "\x02";

		public const string VOLUME_PREFIX = COMMAND_PREFIX + "\x01\x00";
		public const string VOLUME = VOLUME_PREFIX + "\x00";
		public const string VOLUME_UP = VOLUME_PREFIX + "\x01\x00";
		public const string VOLUME_DOWN = VOLUME_PREFIX + "\x02\x00";

		public const string INPUT_PREFIX = COMMAND_PREFIX + "\x0A\x00";
		public const string INPUT_HDMI_1 = INPUT_PREFIX + "\x05\x00";
		public const string INPUT_HDMI_2 = INPUT_PREFIX + "\x05\x01";
		public const string INPUT_HDMI_3 = INPUT_PREFIX + "\x05\x02";
		public const string INPUT_HDMI_4 = INPUT_PREFIX + "\x05\x03";
		public const string INPUT_AV_1 = INPUT_PREFIX + "\x01\x00";
		public const string INPUT_AV_2 = INPUT_PREFIX + "\x01\x01";
		public const string INPUT_AV_3 = INPUT_PREFIX + "\x01\x02";
		public const string INPUT_SVIDEO_1 = INPUT_PREFIX + "\x02\x00";
		public const string INPUT_SVIDEO_2 = INPUT_PREFIX + "\x02\x01";
		public const string INPUT_SVIDEO_3 = INPUT_PREFIX + "\x02\x02";
		public const string INPUT_COMPONENT_1 = INPUT_PREFIX + "\x03\x00";
		public const string INPUT_COMPONENT_2 = INPUT_PREFIX + "\x03\x01";
		public const string INPUT_COMPONENT_3 = INPUT_PREFIX + "\x03\x02";
		public const string INPUT_TV = INPUT_PREFIX + "\x00\x00";

		public const string ART_MODE_PREFIX = COMMAND_PREFIX + "\x0B\x0B\x0E";
		public const string ART_MODE_ON = ART_MODE_PREFIX + "\x01";
		public const string ART_MODE_OFF = ART_MODE_PREFIX + "\x00";


		public const int PRIORITY_POWER_RETRY = 1;
		public const int PRIORITY_POWER_INITIAL = 2;
		public const int PRIORITY_INPUT_RETRY = 3;
		public const int PRIORITY_INPUT_INITIAL = 4;
		public const int PRIORITY_DEFAULT = Int32.MaxValue;
	}
}