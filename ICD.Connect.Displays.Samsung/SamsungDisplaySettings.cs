using System;
using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Samsung
{
	public sealed class SamsungDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "SamsungDisplay";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(SamsungDisplay); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static SamsungDisplaySettings FromXml(string xml)
		{
			SamsungDisplaySettings output = new SamsungDisplaySettings();
			output.ParseXml(xml);
			return output;
		}
	}
}
