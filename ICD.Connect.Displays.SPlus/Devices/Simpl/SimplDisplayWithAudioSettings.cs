using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	[KrangSettings("SimplDisplayWithAudio", typeof(SimplDisplayWithAudio))]
	public sealed class SimplDisplayWithAudioSettings : AbstractSimplDisplaySettings
	{
		private const string MIN_VOLUME_ELEMENT = "MinVolume";
		private const string MAX_VOLUME_ELEMENT = "MaxVolume";
		private const string DEFAULT_VOLUME_ELEMENT = "DefaultVolume";

		[PublicAPI]
		public float? VolumeSafetyMin { get; set; }

		[PublicAPI]
		public float? VolumeSafetyMax { get; set; }

		[PublicAPI]
		public float? VolumeDefault { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(MIN_VOLUME_ELEMENT, IcdXmlConvert.ToString(VolumeSafetyMin));
			writer.WriteElementString(MAX_VOLUME_ELEMENT, IcdXmlConvert.ToString(VolumeSafetyMax));
			writer.WriteElementString(DEFAULT_VOLUME_ELEMENT, IcdXmlConvert.ToString(VolumeDefault));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			VolumeSafetyMin = XmlUtils.TryReadChildElementContentAsInt(xml, MIN_VOLUME_ELEMENT);
			VolumeSafetyMax = XmlUtils.TryReadChildElementContentAsInt(xml, MAX_VOLUME_ELEMENT);
			VolumeDefault = XmlUtils.TryReadChildElementContentAsInt(xml, DEFAULT_VOLUME_ELEMENT);
		}
	}
}
