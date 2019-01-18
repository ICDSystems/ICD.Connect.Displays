using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Devices.ProjectorScreens
{
	[KrangSettings("IrProjectorScreen", typeof(IrProjectorScreenDevice))]
	public sealed class IrProjectorScreenDeviceSettings : AbstractProjectorScreenDeviceSettings
	{
		private const string IR_PORT_ID_ELEMENT = "IrPort";
		
		private const string DISPLAY_ON_COMMAND_ELEMENT = "DisplayOnCommand";
		private const string DISPLAY_OFF_COMMAND_ELEMENT = "DisplayOffCommand";

		[OriginatorIdSettingsProperty(typeof(IIrPort))]
		public int? IrPort { get; set; }

		public string DisplayOnCommand { get; set; }

		public string DisplayOffCommand { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(IR_PORT_ID_ELEMENT, IcdXmlConvert.ToString(IrPort));
			writer.WriteElementString(DISPLAY_ON_COMMAND_ELEMENT, IcdXmlConvert.ToString(DisplayOnCommand));
			writer.WriteElementString(DISPLAY_OFF_COMMAND_ELEMENT, IcdXmlConvert.ToString(DisplayOffCommand));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			IrPort = XmlUtils.TryReadChildElementContentAsInt(xml, IR_PORT_ID_ELEMENT);
			DisplayOnCommand = XmlUtils.TryReadChildElementContentAsString(xml, DISPLAY_ON_COMMAND_ELEMENT);
			DisplayOffCommand = XmlUtils.TryReadChildElementContentAsString(xml, DISPLAY_OFF_COMMAND_ELEMENT);
		}
	}
}