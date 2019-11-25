namespace ICD.Connect.Displays.Settings
{
	public interface IProjectorSettings : IDisplaySettings
	{
		/// <summary>
		/// Warming time for the device, in milliseconds
		/// Defaults to 0
		/// </summary>
		long WarmingTime { get; set; }

		/// <summary>
		/// Cooling time for the device, in milliseconds
		/// Defaults to 0
		/// </summary>
		long CoolingTime { get; set; }

	}
}
