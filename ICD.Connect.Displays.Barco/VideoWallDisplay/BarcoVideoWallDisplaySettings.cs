using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Barco.VideoWallDisplay
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class BarcoVideoWallDisplaySettings : AbstractDisplaySettings
	{
		private const string FACTORY_NAME = "BarcoVideoWallDisplay";

		private const string WALL_DEVICE_ID_ELEMENT = "WallDeviceId";

		public string WallDeviceId { get; set; }

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(BarcoVideoWallDisplay); } }


		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WALL_DEVICE_ID_ELEMENT, IcdXmlConvert.ToString(WallDeviceId));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			WallDeviceId = XmlUtils.TryReadChildElementContentAsString(xml, WALL_DEVICE_ID_ELEMENT);
		}
	}
}