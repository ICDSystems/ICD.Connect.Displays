using ICD.Common.Properties;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Displays.Settings
{
	public abstract class AbstractDisplayWithAudioSettings : AbstractDisplaySettings
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

			if (VolumeSafetyMin != null)
				writer.WriteElementString(MIN_VOLUME_ELEMENT, IcdXmlConvert.ToString((float)VolumeSafetyMin));

			if (VolumeSafetyMax != null)
				writer.WriteElementString(MAX_VOLUME_ELEMENT, IcdXmlConvert.ToString((float)VolumeSafetyMax));

			if (VolumeDefault != null)
				writer.WriteElementString(DEFAULT_VOLUME_ELEMENT, IcdXmlConvert.ToString((float)VolumeDefault));
		}

		/// <summary>
		/// Parses the xml and applies the properties to the instance.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="xml"></param>
		protected static void ParseXml(AbstractDisplayWithAudioSettings instance, string xml)
		{
			int? minVolume = XmlUtils.TryReadChildElementContentAsInt(xml, MIN_VOLUME_ELEMENT);
			if (minVolume != null)
				instance.VolumeSafetyMin = (float)minVolume;

			int? maxVolume = XmlUtils.TryReadChildElementContentAsInt(xml, MAX_VOLUME_ELEMENT);
			if (maxVolume != null)
				instance.VolumeSafetyMax = (float)maxVolume;

			int? defaultVolume = XmlUtils.TryReadChildElementContentAsInt(xml, DEFAULT_VOLUME_ELEMENT);
			if (defaultVolume != null)
				instance.VolumeDefault = (float)defaultVolume;

			AbstractDisplaySettings.ParseXml(instance, xml);
		}
	}
}
