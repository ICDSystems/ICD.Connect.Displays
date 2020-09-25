using ICD.Common.Logging.Activities;
using ICD.Common.Utils.Services.Logging;

namespace ICD.Connect.Displays.Devices
{
	public static class DisplayActivities
	{
		public static Activity GetMutedActivity(bool isMuted)
		{
			return isMuted
				       ? new Activity(Activity.ePriority.Medium, "Is Muted", "Muted", eSeverity.Informational)
				       : new Activity(Activity.ePriority.Low, "Is Muted", "Unmuted", eSeverity.Informational);
		}
	}
}
