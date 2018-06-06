using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Nec.Devices
{
	[KrangSettings("NecDisplay", typeof(NecDisplay))]
	public sealed class NecDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string ELEMENT_MONITOR_ID = "MonitorId";

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

			writer.WriteElementString(ELEMENT_MONITOR_ID, IcdXmlConvert.ToString(MonitorId));
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
