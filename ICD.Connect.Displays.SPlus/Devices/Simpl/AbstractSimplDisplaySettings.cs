using ICD.Common.Utils.Xml;
using ICD.Connect.Devices.CrestronSPlus.Devices.SPlus;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public abstract class AbstractSimplDisplaySettings : AbstractSPlusDeviceSettings
	{
		private const string TRUST_ELEMENT = "Trust";

		public bool Trust { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(TRUST_ELEMENT, IcdXmlConvert.ToString(Trust));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Trust = XmlUtils.TryReadChildElementContentAsBoolean(xml, TRUST_ELEMENT) ?? false;
		}
	}
}
