using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Displays.Settings
{
	public interface IDisplaySettings : IDeviceSettings, ISecureNetworkSettings, IComSpecSettings
	{
		int? Port { get; set; }
	}
}