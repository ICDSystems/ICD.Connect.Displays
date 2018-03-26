﻿using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.Devices
{
	public delegate void DisplayHdmiInputDelegate(IDisplay display, int hdmiInput, bool active);

	/// <summary>
	/// IDisplay provides methods for controlling a TV.
	/// </summary>
	[ApiClass(typeof(ProxyDisplay))]
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
		[ApiProperty(DisplayApi.PROPERTY_IS_POWERED, DisplayApi.HELP_PROPERTY_IS_POWERED)]
		bool IsPowered { get; }

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		[ApiProperty(DisplayApi.PROPERTY_INPUT_COUNT, DisplayApi.HELP_PROPERTY_INPUT_COUNT)]
		int InputCount { get; }

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		[ApiProperty(DisplayApi.PROPERTY_HDMI_INPUT, DisplayApi.HELP_PROPERTY_HDMI_INPUT)]
		int? HdmiInput { get; }

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		[ApiProperty(DisplayApi.PROPERTY_SCALING_MODE, DisplayApi.HELP_PROPERTY_SCALING_MODE)]
		eScalingMode ScalingMode { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		[ApiMethod(DisplayApi.METHOD_POWER_ON, DisplayApi.HELP_METHOD_POWER_ON)]
		void PowerOn();

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		[ApiMethod(DisplayApi.METHOD_POWER_OFF, DisplayApi.HELP_METHOD_POWER_OFF)]
		void PowerOff();

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		[ApiMethod(DisplayApi.METHOD_SET_HDMI_INPUT, DisplayApi.HELP_METHOD_SET_HDMI_INPUT)]
		void SetHdmiInput(int address);

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		[ApiMethod(DisplayApi.METHOD_SET_SCALING_MODE, DisplayApi.HELP_METHOD_SET_SCALING_MODE)]
		void SetScalingMode(eScalingMode mode);

		#endregion
	}
}
