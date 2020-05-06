using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public interface ISamsungProCommand : ISerialData
	{
		byte Command { get; }

		byte Id { get; }
	}
}