using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Devices.Telemetry.DeviceInfo;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public sealed class SamsungProDisplay : AbstractSamsungProDisplay<SamsungProDisplaySettings>
	{
		#region Constants

		private const byte SERIAL = 0x0B;
		private const byte SYSTEM_CONFIGURATION = 0x1B;
		private const byte SOFTWARE_VERSION = 0x0E;

		private const byte SYSTEM_MAC_ADDRESS_CONFIGURATION = 0x81;
		private const byte SYSTEM_NETWORK_CONFIGURATION = 0x82;
		private const byte SYSTEM_IP_MODE_CONFIGURATION = 0x85;

		#endregion

		#region Properties

		/// <summary>
		/// Gets/sets the ID of this tv.
		/// </summary>
		[PublicAPI]
		public byte WallId { get; set; }

		#endregion

		#region Methods

		protected override byte GetWallIdForPowerCommand()
		{
			return WallId;
		}

		protected override byte GetWallIdForInputCommand()
		{
			return WallId;
		}

		protected override byte GetWallIdForVolumeCommand()
		{
			return WallId;
		}

		protected override void QueryState()
		{
			base.QueryState();

			if (PowerState != ePowerState.PowerOn)
				return;

			// Telemetry queries
			SendCommandPriority(new SamsungProCommand(SERIAL, WallId, 0).ToQuery(), int.MinValue);
			SendCommandPriority(new SamsungProCommand(SYSTEM_CONFIGURATION, WallId, SYSTEM_MAC_ADDRESS_CONFIGURATION, new byte[0]).ToQuery(), int.MinValue);
			SendCommandPriority(new SamsungProCommand(SYSTEM_CONFIGURATION, WallId, SYSTEM_NETWORK_CONFIGURATION, new byte[0]).ToQuery(), int.MinValue);
			SendCommandPriority(new SamsungProCommand(SYSTEM_CONFIGURATION, WallId, SYSTEM_IP_MODE_CONFIGURATION, new byte[0]).ToQuery(), int.MinValue);
			SendCommandPriority(new SamsungProCommand(SOFTWARE_VERSION, WallId, 0).ToQuery(), int.MinValue);
		}

		#endregion

		#region Parsing

		protected override void ParseSuccess(SerialResponseEventArgs args)
		{
			base.ParseSuccess(args);

			SamsungProResponse response = new SamsungProResponse(args.Response);
			switch (response.Command)
			{
				case SERIAL:
					MonitoredDeviceInfo.SerialNumber = StringUtils.ToString(response.Values);
					break;

				case SYSTEM_CONFIGURATION:
					switch (response.Subcommand)
					{
						case SYSTEM_MAC_ADDRESS_CONFIGURATION:
							ParseMacAddressConfigurationResponse(response);
							break;
						case SYSTEM_NETWORK_CONFIGURATION:
							ParseNetworkConfigurationResponse(response);
							break;
						case SYSTEM_IP_MODE_CONFIGURATION:
							ParseIpModeConfigurationResponse(response);
							break;
					}
					break;

				case SOFTWARE_VERSION:
					MonitoredDeviceInfo.FirmwareVersion = StringUtils.ToString(response.Values);
					break;
			}
		}

		private void ParseMacAddressConfigurationResponse(SamsungProResponse response)
		{
			try
			{
				IcdPhysicalAddress mac;
				IcdPhysicalAddress.TryParse(StringUtils.ToString(response.Values), out mac);

				MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).MacAddress = mac;
			}
			catch (FormatException e)
			{
				Logger.Log(eSeverity.Error, "Failed to parse SamsungProDisplay MAC Address - {0}", e.Message);
			}
		}

		private void ParseNetworkConfigurationResponse(SamsungProResponse response)
		{
			// Network configuration response values are structured as so:
			// 1st 4 bytes = IPv4 Address
			// 2nd 4 bytes = Subnet
			// 3rd 4 bytes = Gateway
			// 4th 4 bytes = DNS
			byte[] ipBytes = response.Values.Take(4).ToArray();
			byte[] subnetBytes = response.Values.Skip(4).Take(4).ToArray();
			byte[] gatewayBytes = response.Values.Skip(8).Take(4).ToArray();
			byte[] dnsBytes = response.Values.Skip(12).Take(4).ToArray();

			MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4Address =
				ParseNetworkBytesToString(ipBytes);
			MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4SubnetMask =
				ParseNetworkBytesToString(subnetBytes);
			MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4Gateway =
				ParseNetworkBytesToString(gatewayBytes);
			MonitoredDeviceInfo.NetworkInfo.Dns =
				ParseNetworkBytesToString(dnsBytes);
		}

		private string ParseNetworkBytesToString([NotNull] IEnumerable<byte> bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException("bytes");

			try
			{
				return string.Join(".", bytes.Select(b => StringUtils.ToString(b)).ToArray());
			}
			catch (Exception e)
			{
				Logger.Log(eSeverity.Error, "Unable to parse SamsungProDisplay network information - {0}", e.Message);
				return null;
			}
		}

		private void ParseIpModeConfigurationResponse(SamsungProResponse response)
		{
			// dynamic value = 0x00
			// static value = 0x01
			const byte dynamicMode = 0x00;

			MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Dhcp = response.Values[0] == dynamicMode;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			WallId = 0;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SamsungProDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.WallId = WallId;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SamsungProDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			WallId = settings.WallId;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Wall ID", WallId);
		}

		#endregion
	}
}