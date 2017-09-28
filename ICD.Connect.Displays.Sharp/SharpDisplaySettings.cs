using System;
using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sharp
{
	/// <summary>
	/// Settings for the SharpDisplay device.
	/// </summary>
	public sealed class SharpDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "SharpDisplay";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(SharpDisplay); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static SharpDisplaySettings FromXml(string xml)
		{
			SharpDisplaySettings output = new SharpDisplaySettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
