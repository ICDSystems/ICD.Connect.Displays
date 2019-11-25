using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.Devices
{
	public interface IProjector : IDisplay
	{
		[ApiEvent(ProjectorApi.EVENT_LAMP_HOURS, ProjectorApi.HELP_EVENT_LAMP_HOURS)]
		event EventHandler<ProjectorLampHoursApiEventArgs> OnLampHoursUpdated;  

		[ApiProperty(ProjectorApi.PROPERTY_LAMP_HOURS, ProjectorApi.HELP_PROPERTY_LAMP_HOURS)]
		int LampHours { get; }
	}
}