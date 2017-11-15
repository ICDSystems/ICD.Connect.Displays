using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Settings
{
	public abstract class AbstractDisplaySettings : AbstractDeviceSettings
	{
		private const string PORT_ELEMENT = "Port";

		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, Port == null ? null : IcdXmlConvert.ToString((int)Port));
		}

		/// <summary>
		/// Parses the xml and applies the properties to the instance.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="xml"></param>
		protected static void ParseXml(AbstractDisplaySettings instance, string xml)
		{
			instance.Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);

			AbstractDeviceSettings.ParseXml(instance, xml);
		}
	}
}
