using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public abstract class AbstractSamsungProDisplaySettings : AbstractDisplayWithAudioSettings, ISamsungProDisplaySettings
	{
		public const string DISABLE_LAUCHER_ELEMENT = "DisableLauncher";

		/// <summary>
		/// Set to true to disable URL Launher if not supported on the display.
		/// </summary>
		public bool DisableLauncher { get; set; }

		/// <summary>
		/// Sets default values for unconfigured network properties.
		/// </summary>
		/// <param name="networkProperties"></param>
		protected override void UpdateNetworkDefaults(SecureNetworkProperties networkProperties)
		{
			if (networkProperties == null)
				throw new ArgumentNullException("networkProperties");

			networkProperties.ApplyDefaultValues(null, 1515);
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

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(DISABLE_LAUCHER_ELEMENT, IcdXmlConvert.ToString(DisableLauncher));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			DisableLauncher = XmlUtils.TryReadChildElementContentAsBoolean(xml, DISABLE_LAUCHER_ELEMENT) ?? false;
		}
	}
}
