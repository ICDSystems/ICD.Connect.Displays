using System;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.Planar.Devices.PlanarQe
{
	[KrangSettings("PlanarQe", typeof(PlanarQeDisplay))]
	public sealed class PlanarQeDisplaySettings : AbstractDisplayWithAudioSettings
	{
		/// <summary>
		/// Sets default values for unconfigured network properties.
		/// </summary>
		/// <param name="networkProperties"></param>
		protected override void UpdateNetworkDefaults(SecureNetworkProperties networkProperties)
		{
			if (networkProperties == null)
				throw new ArgumentNullException("networkProperties");

			networkProperties.ApplyDefaultValues(null, 57);
		}

		/// <summary>
		/// Sets default values for unconfigured comspec properties.
		/// </summary>
		/// <param name="comSpecProperties"></param>
		protected override void UpdateComSpecDefaults(ComSpecProperties comSpecProperties)
		{
			if (comSpecProperties == null)
				throw new ArgumentNullException("comSpecProperties");

			comSpecProperties.ApplyDefaultValues(eComBaudRates.BaudRate19200,
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
