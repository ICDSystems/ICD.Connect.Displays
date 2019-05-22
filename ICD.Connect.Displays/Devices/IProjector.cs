using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Displays.Devices
{
	public interface IProjector : IDisplay
	{
		event EventHandler OnLampHoursUpdated;  

		int GetLampHours();
	}
}