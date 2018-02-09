using System;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Christie
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class ChristieDisplaySettings : AbstractDisplaySettings
	{
		private const string FACTORY_NAME = "ChristieDisplay";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(ChristieDisplay); } }
	}
}
