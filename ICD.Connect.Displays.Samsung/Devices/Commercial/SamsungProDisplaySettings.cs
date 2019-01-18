using System;
﻿using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	/// <summary>
	/// Settings for the SamsungProDisplay device.
	/// </summary>
	[KrangSettings("SamsungProDisplay", typeof(SamsungProDisplay))]
	public sealed class SamsungProDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string WALLID_ELEMENT = "WallId";

		/// <summary>
		/// The video wall id for this display.
		/// </summary>
		[CrestronByteSettingsProperty]
		public byte WallId { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WALLID_ELEMENT, StringUtils.ToIpIdString(WallId));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			WallId = XmlUtils.TryReadChildElementContentAsByte(xml, WALLID_ELEMENT) ?? 0;
		}

		/// <summary>
		/// Sets default values for unconfigured network properties.
		/// </summary>
		/// <param name="networkProperties"></param>
		protected override void UpdateNetworkDefaults(SecureNetworkProperties networkProperties)
		{
			if (networkProperties == null)
				throw new ArgumentNullException("networkProperties");
		}

		/// <summary>
		/// Sets default values for unconfigured comspec properties.
		/// </summary>
		/// <param name="comSpecProperties"></param>
		protected override void UpdateComSpecDefaults(ComSpecProperties comSpecProperties)
		{
			if (comSpecProperties == null)
				throw new ArgumentNullException("comSpecProperties");

			comSpecProperties.ApplyDefaultValues(eComBaudRates.BaudRate9600,
			                                     eComDataBits.DataBits8,
			                                     eComParityType.None,
			                                     eComStopBits.StopBits1,
			                                     eComProtocolType.Rs232,
			                                     eComHardwareHandshakeType.None,
			                                     eComSoftwareHandshakeType.None,
			                                     false);
		}
	}
}
