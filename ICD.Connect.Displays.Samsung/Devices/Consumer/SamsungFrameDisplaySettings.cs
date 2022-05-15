using ICD.Common.Utils.Xml;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	[KrangSettings("SamsungFrameDisplay", typeof(SamsungFrameDisplay))]
	public sealed class SamsungFrameDisplaySettings : AbstractSamsungDisplaySettings
	{
		private const string ELEMENT_ART_MODE_DEFAUT = "ArtModeDefault";

		public bool ArtModeDefault { get; set; }

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			ArtModeDefault = XmlUtils.TryReadChildElementContentAsBoolean(xml, ELEMENT_ART_MODE_DEFAUT) ?? false;
		}

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_ART_MODE_DEFAUT, IcdXmlConvert.ToString(ArtModeDefault));
		}
	}
}