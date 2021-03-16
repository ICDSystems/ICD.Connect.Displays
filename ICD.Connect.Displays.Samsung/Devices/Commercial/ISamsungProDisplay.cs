using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Displays.Devices;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public interface ISamsungProDisplay : IDisplayWithAudio
	{
		event EventHandler<GenericEventArgs<Uri>> OnUrlLauncherSourceChanged;

		void SetUrlLauncherSource(Uri source);

		[CanBeNull]
		Uri LauncherUri { get; }
	}
}
