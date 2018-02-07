using System;
using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Nec
{
	public sealed class NecDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "NecDisplay";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(NecDisplay); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static NecDisplaySettings FromXml(string xml)
		{
			NecDisplaySettings output = new NecDisplaySettings();
			output.ParseXml(xml);
			return output;
		}
	}
}
