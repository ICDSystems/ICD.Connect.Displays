using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

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
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			NecDisplay output = new NecDisplay();
			output.ApplySettings(this, factory);
			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlDeviceSettingsFactoryMethod(FACTORY_NAME)]
		public static NecDisplaySettings FromXml(string xml)
		{
			NecDisplaySettings output = new NecDisplaySettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
