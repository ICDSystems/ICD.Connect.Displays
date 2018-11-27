using System;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.SmartTech.Devices
{
	[KrangSettings("SmartTechDisplay", typeof(SmartTechDisplay))]
	public sealed class SmartTechDisplaySettings : AbstractDisplayWithAudioSettings
	{
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

			comSpecProperties.ApplyDefaultValues(eComBaudRates.ComspecBaudRate19200,
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
