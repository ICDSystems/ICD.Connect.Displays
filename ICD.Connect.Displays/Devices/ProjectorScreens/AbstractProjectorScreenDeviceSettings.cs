using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Devices.ProjectorScreens
{
	public abstract class AbstractProjectorScreenDeviceSettings : AbstractDeviceSettings
	{

		private const string DISPLAY_ID_ELEMENT = "Display";


		[OriginatorIdSettingsProperty(typeof(IDisplay))]
		public int? Display { get; set; }


		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(DISPLAY_ID_ELEMENT, IcdXmlConvert.ToString(Display));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Display = XmlUtils.TryReadChildElementContentAsInt(xml, DISPLAY_ID_ELEMENT);
		}
	}
}