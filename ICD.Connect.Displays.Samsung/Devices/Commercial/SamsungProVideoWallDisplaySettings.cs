using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	[KrangSettings("SamsungProVideoWallDisplay", typeof(SamsungProVideoWallDisplay))]
	public sealed class SamsungProVideoWallDisplaySettings : AbstractSamsungProDisplaySettings
	{
		private const string INPUT_WALLID_ELEMENT = "InputWallId";
		private const string VOLUME_WALLID_ELEMENT = "VolumeWallId";

		[CrestronByteSettingsProperty]
		public byte? InputWallId { get; set; }

		[CrestronByteSettingsProperty]
		public byte? VolumeWallId { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			if (InputWallId.HasValue)
				writer.WriteElementString(INPUT_WALLID_ELEMENT, StringUtils.ToIpIdString(InputWallId.Value));
			if (VolumeWallId.HasValue)
				writer.WriteElementString(VOLUME_WALLID_ELEMENT, StringUtils.ToIpIdString(VolumeWallId.Value));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			InputWallId = XmlUtils.TryReadChildElementContentAsByte(xml, INPUT_WALLID_ELEMENT);
			VolumeWallId = XmlUtils.TryReadChildElementContentAsByte(xml, VOLUME_WALLID_ELEMENT);
		}
	}
}
