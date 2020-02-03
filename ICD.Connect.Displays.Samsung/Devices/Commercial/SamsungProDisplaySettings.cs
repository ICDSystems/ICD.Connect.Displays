using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	/// <summary>
	/// Settings for the SamsungProDisplay device.
	/// </summary>
	[KrangSettings("SamsungProDisplay", typeof(SamsungProDisplay))]
	public sealed class SamsungProDisplaySettings : AbstractSamsungProDisplaySettings
	{
		private const string WALLID_ELEMENT = "WallId";

		/// <summary>
		/// The video wall id for this display.
		/// </summary>
		[CrestronByteSettingsProperty]
		public byte WallId { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WALLID_ELEMENT, StringUtils.ToIpIdString(WallId));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			WallId = XmlUtils.TryReadChildElementContentAsByte(xml, WALLID_ELEMENT) ?? 0;
		}
	}
}
