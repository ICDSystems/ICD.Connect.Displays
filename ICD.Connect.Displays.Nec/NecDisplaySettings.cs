using System;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Nec
{
	[KrangSettings(FACTORY_NAME)]
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
	}
}
