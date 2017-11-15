using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Samsung
{
	/// <summary>
	/// Settings for the SamsungProDisplay device.
	/// </summary>
	public sealed class SamsungProDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "SamsungProDisplay";

		private const string WALLID_ELEMENT = "WallId";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(SamsungProDisplay); } }

		/// <summary>
		/// The video wall id for this display.
		/// </summary>
		[IpIdSettingsProperty]
		public byte WallId { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WALLID_ELEMENT, IcdXmlConvert.ToString(WallId));
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static SamsungProDisplaySettings FromXml(string xml)
		{
			SamsungProDisplaySettings output = new SamsungProDisplaySettings
			{
				WallId = XmlUtils.TryReadChildElementContentAsByte(xml, WALLID_ELEMENT) ?? 0
			};

			ParseXml(output, xml);
			return output;
		}
	}
}
