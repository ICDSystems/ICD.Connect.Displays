using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Nec
{
	[KrangSettings("NecDisplay", typeof(NecDisplay))]
	public sealed class NecDisplaySettings : AbstractDisplayWithAudioSettings
	{
		private const string ELEMENT_MONITOR_ID = "MonitorId";

		/// <summary>
		/// The id for the monitor in a video wall.
		/// </summary>
		public byte? MonitorId { get; set; }

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ELEMENT_MONITOR_ID, IcdXmlConvert.ToString(MonitorId));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			MonitorId = XmlUtils.TryReadChildElementContentAsByte(xml, ELEMENT_MONITOR_ID);
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

			comSpecProperties.ApplyDefaultValues(eComBaudRates.ComspecBaudRate9600,
			                                     eComDataBits.ComspecDataBits8,
			                                     eComParityType.ComspecParityNone,
			                                     eComStopBits.ComspecStopBits1,
			                                     eComProtocolType.ComspecProtocolRS232,
			                                     eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
			                                     eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
			                                     false);
		}
	}
}
