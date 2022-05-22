using ICD.Common.Utils.Xml;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	[KrangSettings("SamsungFrameDisplay", typeof(SamsungFrameDisplay))]
	public sealed class SamsungFrameDisplaySettings : AbstractSamsungDisplaySettings
	{
		private const string ELEMENT_ART_MODE_DEFAUT = "ArtModeDefault";
		private const string ELEMNET_ART_MODE_AT_POWER_ON = "ArtModeAtPowerOn";

		/// <summary>
		/// What the default setting for art mode should be on load
		/// </summary>
		public bool ArtModeDefault { get; set; }

		/// <summary>
		/// If set, specifies what art mode should be set to when the display powers on
		/// </summary>
		public bool? ArtModeAtPowerOn { get; set; }

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			ArtModeDefault = XmlUtils.TryReadChildElementContentAsBoolean(xml, ELEMENT_ART_MODE_DEFAUT) ?? false;
			ArtModeAtPowerOn = XmlUtils.TryReadChildElementContentAsBoolean(xml, ELEMNET_ART_MODE_AT_POWER_ON);
		}

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_ART_MODE_DEFAUT, IcdXmlConvert.ToString(ArtModeDefault));
			writer.WriteElementString(ELEMNET_ART_MODE_AT_POWER_ON, IcdXmlConvert.ToString(ArtModeAtPowerOn));
		}
	}
}