using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Barco.VideoWallDisplay
{
	[KrangSettings("BarcoVideoWallDisplay", typeof(BarcoVideoWallDisplay))]
	public sealed class BarcoVideoWallDisplaySettings : AbstractDisplaySettings
	{
		private const string WALL_DEVICE_ID_ELEMENT = "WallDeviceId";

		private const string WALL_INPUT_CONTROL_DEVICE = "InputControlDevice";

		public string WallDeviceId { get; set; }

		public string WallInputControlDevice { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WALL_DEVICE_ID_ELEMENT, IcdXmlConvert.ToString(WallDeviceId));
			writer.WriteElementString(WALL_INPUT_CONTROL_DEVICE, IcdXmlConvert.ToString(WallInputControlDevice));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			WallDeviceId = XmlUtils.TryReadChildElementContentAsString(xml, WALL_DEVICE_ID_ELEMENT);
			WallInputControlDevice = XmlUtils.TryReadChildElementContentAsString(xml, WALL_INPUT_CONTROL_DEVICE) ?? "1,1";
		}
	}
}