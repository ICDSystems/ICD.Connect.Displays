using System;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Proxies;

namespace ICD.Connect.Displays.SPlus.Proxy
{
	public class AbstractProxySimplDisplaySettings : AbstractProxySettings, IProxySimplDisplaySettings
	{
		/// <summary>
		/// Gets/sets the manufacturer for this device.
		/// </summary>
		string IDeviceBaseSettings.Manufacturer { get; set; }

		/// <summary>
		/// Gets/sets the model number for this device.
		/// </summary>
		string IDeviceBaseSettings.Model { get; set; }

		/// <summary>
		/// Gets/sets the serial number for this device.
		/// </summary>
		string IDeviceBaseSettings.SerialNumber { get; set; }

		/// <summary>
		/// Gets/sets the purchase date for this device.
		/// </summary>
		DateTime IDeviceBaseSettings.PurchaseDate { get; set; }
	}
}
