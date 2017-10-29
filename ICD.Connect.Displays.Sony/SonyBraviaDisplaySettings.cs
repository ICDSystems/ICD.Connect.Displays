using System;
using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sony
{
	public sealed class SonyBraviaDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "SonyBraviaDisplay";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		public override Type OriginatorType { get { return typeof(SonyBraviaDisplay); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static SonyBraviaDisplaySettings FromXml(string xml)
		{
			SonyBraviaDisplaySettings output = new SonyBraviaDisplaySettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
