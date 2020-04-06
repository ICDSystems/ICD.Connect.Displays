using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Mock;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Mock.Devices
{
	/// <summary>
	/// Settings for the MockDisplayWithAudio device.
	/// </summary>
	[KrangSettings("MockDisplayWithAudio", typeof(MockDisplayWithAudio))]
	public sealed class MockDisplayWithAudioSettings : AbstractMockDeviceSettings
	{
		private const string WARMING_TIME_ELEMENT = "WarmingTime";
		private const string COOLING_TIME_ELEMENT = "CoolingTime";

		/// <summary>
		/// Warming time for the device, in milliseconds
		/// Defaults to 0
		/// </summary>
		public long WarmingTime { get; set; }

		/// <summary>
		/// Cooling time for the device, in milliseconds
		/// Defaults to 0
		/// </summary>
		public long CoolingTime { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WARMING_TIME_ELEMENT, IcdXmlConvert.ToString(WarmingTime));
			writer.WriteElementString(COOLING_TIME_ELEMENT, IcdXmlConvert.ToString(CoolingTime));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			WarmingTime = XmlUtils.TryReadChildElementContentAsLong(xml, WARMING_TIME_ELEMENT) ?? 0;
			CoolingTime = XmlUtils.TryReadChildElementContentAsLong(xml, COOLING_TIME_ELEMENT) ?? 0;
		}
	}
}
