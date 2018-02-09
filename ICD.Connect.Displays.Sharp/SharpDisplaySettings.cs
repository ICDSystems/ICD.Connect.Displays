using System;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Sharp
{
	/// <summary>
	/// Settings for the SharpDisplay device.
	/// </summary>
	[KrangSettings(FACTORY_NAME)]
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
	}
}
