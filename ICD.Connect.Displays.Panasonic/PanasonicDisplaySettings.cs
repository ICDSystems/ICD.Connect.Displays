using System;
using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Panasonic
{
	public sealed class PanasonicDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "PanasonicDisplay";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(PanasonicDisplay); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static PanasonicDisplaySettings FromXml(string xml)
		{
			PanasonicDisplaySettings output = new PanasonicDisplaySettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
