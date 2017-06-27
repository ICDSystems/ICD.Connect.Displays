using System;
using ICD.Common.Attributes.Properties;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

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
		[SettingsProperty(SettingsProperty.ePropertyType.Ipid)]
		public byte WallId { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			if (WallId != 0)
				writer.WriteElementString(WALLID_ELEMENT, IcdXmlConvert.ToString(WallId));
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlDeviceSettingsFactoryMethod(FACTORY_NAME)]
		public static SamsungProDisplaySettings FromXml(string xml)
		{
			byte? wallId = XmlUtils.TryReadChildElementContentAsByte(xml, WALLID_ELEMENT);

			SamsungProDisplaySettings output = new SamsungProDisplaySettings
			{
				WallId = wallId == null ? (byte)0 : (byte)wallId
			};

			ParseXml(output, xml);
			return output;
		}
	}
}
