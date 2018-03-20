using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Devices
{
	public delegate void DisplayHdmiInputDelegate(IDisplay display, int hdmiInput, bool active);

	/// <summary>
	/// IDisplay provides methods for controlling a TV.
	/// </summary>
	public interface IDisplay : IDevice
	{
		#region Events

		event EventHandler<BoolEventArgs> OnIsPoweredChanged;
		event DisplayHdmiInputDelegate OnHdmiInputChanged;
		event EventHandler<ScalingModeEventArgs> OnScalingModeChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		bool IsPowered { get; }

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		int InputCount { get; }

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		int? HdmiInput { get; }

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		eScalingMode ScalingMode { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		void PowerOn();

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		void PowerOff();

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		void SetHdmiInput(int address);

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		void SetScalingMode(eScalingMode mode);

		#endregion
	}
}
