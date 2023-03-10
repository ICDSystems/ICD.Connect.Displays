using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Barco.VideoWallDisplay
{
	[KrangSettings("BarcoVideoWallDisplay", typeof(BarcoVideoWallDisplay))]
	public sealed class BarcoVideoWallDisplaySettings : AbstractDisplaySettings
	{
		private const string WALL_DEVICE_ID_ELEMENT = "WallDeviceId";

		private const string WALL_INPUT_CONTROL_DEVICE = "InputControlDevice";

		/// <summary>
		/// Wall device ID, as set on the Barco BCM
		/// </summary>
		public string WallDeviceId { get; set; }

		/// <summary>
		/// Wall device to control the input on
		/// This will typically be a single device,
		/// with other devices cascaded off this one.
		/// Can be set to "wall" in the rare event the entire wall needs to switch inputs
		/// Defaults to "1,1" for the top-left display
		/// </summary>
		public string WallInputControlDevice { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WALL_DEVICE_ID_ELEMENT, IcdXmlConvert.ToString(WallDeviceId));
			writer.WriteElementString(WALL_INPUT_CONTROL_DEVICE, IcdXmlConvert.ToString(WallInputControlDevice));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			WallDeviceId = XmlUtils.TryReadChildElementContentAsString(xml, WALL_DEVICE_ID_ELEMENT);
			WallInputControlDevice = XmlUtils.TryReadChildElementContentAsString(xml, WALL_INPUT_CONTROL_DEVICE) ?? "1,1";
		}

		/// <summary>
		/// Sets default values for unconfigured network properties.
		/// </summary>
		/// <param name="networkProperties"></param>
		protected override void UpdateNetworkDefaults(SecureNetworkProperties networkProperties)
		{
			if (networkProperties == null)
				throw new ArgumentNullException("networkProperties");

			networkProperties.ApplyDefaultValues(null, 23500);
		}

		/// <summary>
		/// Sets default values for unconfigured comspec properties.
		/// </summary>
		/// <param name="comSpecProperties"></param>
		protected override void UpdateComSpecDefaults(ComSpecProperties comSpecProperties)
		{
		}
	}
}