using ICD.Connect.Devices.Simpl;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public delegate void SimplDisplayPowerOnCallback(ISimplDisplay sender);

	public delegate void SimplDisplayPowerOffCallback(ISimplDisplay sender);

	public delegate void SimplDisplaySetHdmiInputCallback(ISimplDisplay sender, int address);

	public delegate void SimplDisplaySetScalingModeCallback(ISimplDisplay sender, eScalingMode scalingMode);

	public interface ISimplDisplay : ISimplDevice, IDisplay
	{
		SimplDisplayPowerOnCallback PowerOnCallback { get; set; }

		SimplDisplayPowerOffCallback PowerOffCallback { get; set; }

		SimplDisplaySetHdmiInputCallback SetHdmiInputCallback { get; set; }

		SimplDisplaySetScalingModeCallback SetScalingModeCallback { get; set; }

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		new bool IsPowered { get; set; }

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		new int InputCount { get; set; }

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		new int? HdmiInput { get; set; }

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		new eScalingMode ScalingMode { get; set; }
	}
}
