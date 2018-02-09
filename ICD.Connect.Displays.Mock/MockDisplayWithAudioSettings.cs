using System;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Mock
{
	/// <summary>
	/// Settings for the MockDisplayWithAudio device.
	/// </summary>
	[KrangSettings(FACTORY_NAME)]
	public sealed class MockDisplayWithAudioSettings : AbstractDisplayWithAudioSettings
	{
		private const string FACTORY_NAME = "MockDisplayWithAudio";

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		public override Type OriginatorType { get { return typeof (MockDisplayWithAudio); } }
	}
}
