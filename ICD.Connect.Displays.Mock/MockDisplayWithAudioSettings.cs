using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Mock
{
	/// <summary>
	/// Settings for the MockDisplayWithAudio device.
	/// </summary>
	public sealed class MockDisplayWithAudioSettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "MockDisplayWithAudio";

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
			MockDisplayWithAudio output = new MockDisplayWithAudio();
			output.ApplySettings(this, factory);

			return output;
		}

		/// <summary>
		/// Loads the settings from XML.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		[PublicAPI, XmlDeviceSettingsFactoryMethod(FACTORY_NAME)]
		public static MockDisplayWithAudioSettings FromXml(string xml)
		{
			MockDisplayWithAudioSettings output = new MockDisplayWithAudioSettings();
			ParseXml(output, xml);
			return output;
		}
	}
}
