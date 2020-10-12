using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Devices.ProjectorScreens
{
	[KrangSettings("RelayProjectorScreen", typeof(RelayProjectorScreenDevice))]
	public sealed class RelayProjectorScreenDeviceSettings : AbstractProjectorScreenDeviceSettings
	{
		private const string DISPLAY_ON_RELAY_ID_ELEMENT = "DisplayOnRelay";
		private const string DISPLAY_OFF_RELAY_ID_ELEMENT = "DisplayOffRelay";
		private const string RELAY_LATCH_ELEMENT = "LatchRelay";
		private const string RELAY_HOLD_TIME_ELEMENT = "RelayHoldTime";

		private const bool RELAY_LATCH_DEFAULT = false;
		private const int RELAY_HOLD_TIME_DEFAULT = 500;

		[OriginatorIdSettingsProperty(typeof(IRelayPort))]
		public int? DisplayOnRelay { get; set; }

		[OriginatorIdSettingsProperty(typeof(IRelayPort))]
		public int? DisplayOffRelay { get; set; }

		public bool RelayLatch { get; set; }

		public int RelayHoldTime { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(DISPLAY_OFF_RELAY_ID_ELEMENT, IcdXmlConvert.ToString(DisplayOffRelay));
			writer.WriteElementString(DISPLAY_ON_RELAY_ID_ELEMENT, IcdXmlConvert.ToString(DisplayOnRelay));
			writer.WriteElementString(RELAY_LATCH_ELEMENT, IcdXmlConvert.ToString(RelayLatch));
			writer.WriteElementString(RELAY_HOLD_TIME_ELEMENT, IcdXmlConvert.ToString(RelayHoldTime));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			DisplayOffRelay = XmlUtils.TryReadChildElementContentAsInt(xml, DISPLAY_OFF_RELAY_ID_ELEMENT);
			DisplayOnRelay = XmlUtils.TryReadChildElementContentAsInt(xml, DISPLAY_ON_RELAY_ID_ELEMENT);
			RelayLatch = XmlUtils.TryReadChildElementContentAsBoolean(xml, RELAY_LATCH_ELEMENT) ?? RELAY_LATCH_DEFAULT;
			RelayHoldTime = XmlUtils.TryReadChildElementContentAsInt(xml, RELAY_HOLD_TIME_ELEMENT) ?? RELAY_HOLD_TIME_DEFAULT;
		}
	}
}