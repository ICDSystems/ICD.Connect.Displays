using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

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
		/// Creates a new originator instance from the settings.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public override IOriginator ToOriginator(IDeviceFactory factory)
		{
			SharpDisplay output = new SharpDisplay();
			output.ApplySettings(this, factory);

			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlDeviceSettingsFactoryMethod(FACTORY_NAME)]
		public static SharpDisplaySettings FromXml(string xml)
		{
			SharpDisplaySettings output = new SharpDisplaySettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
