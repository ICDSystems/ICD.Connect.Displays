using System;
using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Christie.JSeries
{
	public sealed class ChristieJSeriesDisplaySettings : AbstractDisplaySettings
	{
		private const string FACTORY_NAME = "ChristieJSeriesDisplay";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(ChristieJSeriesDisplay); } }

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static ChristieJSeriesDisplaySettings FromXml(string xml)
		{
			ChristieJSeriesDisplaySettings output = new ChristieJSeriesDisplaySettings();
			output.ParseXml(xml);
			return output;
		}
	}
}
