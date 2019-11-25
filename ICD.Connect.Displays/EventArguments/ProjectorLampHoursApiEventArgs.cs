using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public sealed class ProjectorLampHoursApiEventArgs : AbstractGenericApiEventArgs<int>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ProjectorLampHoursApiEventArgs(int data) : base(ProjectorApi.EVENT_LAMP_HOURS, data)
		{
		}
	}
}
