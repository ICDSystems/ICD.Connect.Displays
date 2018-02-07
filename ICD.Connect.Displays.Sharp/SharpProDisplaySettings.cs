using System;
using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sharp
{
	/// <summary>
	/// Settings for the SharpProDisplay device.
	/// </summary>
	public sealed class SharpProDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "SharpProDisplay";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(SharpProDisplay); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static SharpProDisplaySettings FromXml(string xml)
		{
			SharpProDisplaySettings output = new SharpProDisplaySettings();
			output.ParseXml(xml);
			return output;
		}
	}
}
