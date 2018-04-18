using System;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Devices.Simpl
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class SimplDisplayWithAudioSettings : AbstractSimplDisplaySettings
	{
		private const string FACTORY_NAME = "SimplDisplayWithAudio";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(SimplDisplayWithAudio); } }
	}
}