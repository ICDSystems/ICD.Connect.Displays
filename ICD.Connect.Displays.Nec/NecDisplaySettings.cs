using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Nec
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class NecDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "NecDisplay";

		private const string ELEMENT_MONITOR_ID = "MonitorId";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(NecDisplay); } }

		/// <summary>
		/// The id for the monitor in a video wall.
		/// </summary>
		public byte? MonitorId { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			if (MonitorId != null)
				writer.WriteElementString(ELEMENT_MONITOR_ID, StringUtils.ToIpIdString((byte)MonitorId));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			MonitorId = XmlUtils.TryReadChildElementContentAsByte(xml, ELEMENT_MONITOR_ID);
		}
	}
}
