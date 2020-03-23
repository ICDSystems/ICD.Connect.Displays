﻿using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;
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
		[ApiEvent(DisplayApi.EVENT_POWER_STATE, DisplayApi.HELP_EVENT_POWER_STATE)]
		[EventTelemetry(DeviceTelemetryNames.POWER_STATE_CHANGED)]
		event EventHandler<DisplayPowerStateApiEventArgs> OnPowerStateChanged;

		/// <summary>
		/// Raised when the active input changes.
		/// </summary>
		[ApiEvent(DisplayApi.EVENT_ACTIVE_INPUT, DisplayApi.HELP_EVENT_ACTIVE_INPUT)]
		[EventTelemetry(DisplayTelemetryNames.ACTIVE_INPUT_STATE_CHANGED)]
		event EventHandler<DisplayInputApiEventArgs> OnActiveInputChanged;

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
		[ApiProperty(DisplayApi.PROPERTY_POWER_STATE, DisplayApi.HELP_PROPERTY_POWER_STATE)]
		[DynamicPropertyTelemetry(DeviceTelemetryNames.POWER_STATE, DeviceTelemetryNames.POWER_STATE_CHANGED)]
		ePowerState PowerState { get; }

		/// <summary>
		/// Gets the active input.
		/// </summary>
		[ApiProperty(DisplayApi.PROPERTY_ACTIVE_INPUT, DisplayApi.HELP_PROPERTY_ACTIVE_INPUT)]
		[DynamicPropertyTelemetry(DisplayTelemetryNames.ACTIVE_INPUT_STATE, DisplayTelemetryNames.ACTIVE_INPUT_STATE_CHANGED)]
		int? ActiveInput { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		[ApiMethod(DisplayApi.METHOD_POWER_ON, DisplayApi.HELP_METHOD_POWER_ON)]
		[MethodTelemetry(DeviceTelemetryNames.POWER_ON)]
		void PowerOn();

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		[ApiMethod(DisplayApi.METHOD_POWER_OFF, DisplayApi.HELP_METHOD_POWER_OFF)]
		[MethodTelemetry(DeviceTelemetryNames.POWER_OFF)]
		void PowerOff();

		/// <summary>
		/// Sets the active input of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		[ApiMethod(DisplayApi.METHOD_SET_ACTIVE_INPUT, DisplayApi.HELP_METHOD_SET_ACTIVE_INPUT)]
		[MethodTelemetry(DisplayTelemetryNames.SET_ACTIVE_INPUT)]
		void SetActiveInput(int address);

		#endregion
	}
}
