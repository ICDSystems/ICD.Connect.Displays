﻿using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Proxies;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Displays.Devices
{
	/// <summary>
	/// IDisplay provides methods for controlling a TV.
	/// </summary>
	[ApiClass(typeof(ProxyDisplay), typeof(IDevice))]
	public interface IDisplay : IDevice
	{
		#region Events

		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		[ApiEvent(DisplayApi.EVENT_IS_POWERED, DisplayApi.HELP_EVENT_IS_POWERED)]
		[EventTelemetry("OnIsPoweredChanged")]
		event EventHandler<DisplayPowerStateApiEventArgs> OnIsPoweredChanged;

		/// <summary>
		/// Raised when the active input changes.
		/// </summary>
		[ApiEvent(DisplayApi.EVENT_ACTIVE_INPUT, DisplayApi.HELP_EVENT_ACTIVE_INPUT)]
		event EventHandler<DisplayInputApiEventArgs> OnActiveInputChanged;

		/// <summary>
		/// Raised when the scaling mode changes.
		/// </summary>
		[ApiEvent(DisplayApi.EVENT_SCALING_MODE, DisplayApi.HELP_EVENT_SCALING_MODE)]
		event EventHandler<DisplayScalingModeApiEventArgs> OnScalingModeChanged;

		#endregion

		#region Properties

		/// <summary>
		/// When true assume TX is successful even if a request times out.
		/// </summary>
		[ApiProperty(DisplayApi.PROPERTY_TRUST, DisplayApi.HELP_PROPERTY_TRUST)]
		bool Trust { get; set; }

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		[ApiProperty(DisplayApi.PROPERTY_IS_POWERED, DisplayApi.HELP_PROPERTY_IS_POWERED)]
		[DynamicPropertyTelemetry("IsPowered", "OnIsPoweredChanged")]
		bool IsPowered { get; }

		/// <summary>
		/// Gets the active input.
		/// </summary>
		[ApiProperty(DisplayApi.PROPERTY_ACTIVE_INPUT, DisplayApi.HELP_PROPERTY_ACTIVE_INPUT)]
		[UpdatablePropertyTelemetry("ActiveInput")]
		int? ActiveInput { get; }

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		[ApiProperty(DisplayApi.PROPERTY_SCALING_MODE, DisplayApi.HELP_PROPERTY_SCALING_MODE)]
		[StaticPropertyTelemetry("ScalingMode")]
		eScalingMode ScalingMode { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		[ApiMethod(DisplayApi.METHOD_POWER_ON, DisplayApi.HELP_METHOD_POWER_ON)]
		[MethodTelemetry("PowerOn")]
		void PowerOn();

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		[ApiMethod(DisplayApi.METHOD_POWER_OFF, DisplayApi.HELP_METHOD_POWER_OFF)]
		[MethodTelemetry("PowerOff")]
		void PowerOff();

		/// <summary>
		/// Sets the active input of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		[ApiMethod(DisplayApi.METHOD_SET_ACTIVE_INPUT, DisplayApi.HELP_METHOD_SET_ACTIVE_INPUT)]
		[MethodTelemetry("SetActiveInput")]
		void SetActiveInput(int address);

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		[ApiMethod(DisplayApi.METHOD_SET_SCALING_MODE, DisplayApi.HELP_METHOD_SET_SCALING_MODE)]
		void SetScalingMode(eScalingMode mode);

		#endregion
	}
}
