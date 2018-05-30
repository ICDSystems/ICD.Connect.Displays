using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices.Simpl;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public abstract class AbstractSimplDisplaySettings : AbstractSimplDeviceSettings
	{
		private const string INPUT_COUNT_ELEMENT = "InputCount";

		[PublicAPI]
		public int InputCount { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(INPUT_COUNT_ELEMENT, IcdXmlConvert.ToString(InputCount));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			InputCount = XmlUtils.TryReadChildElementContentAsInt(xml, INPUT_COUNT_ELEMENT) ?? 0;
		}
	}
}
