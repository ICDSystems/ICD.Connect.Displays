using System;

namespace ICD.Connect.Displays.Devices
{
	public interface IProjector : IDisplay
	{
		event EventHandler OnLampHoursUpdated;  

		int GetLampHours();
	}
}