using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays
{
	public abstract class AbstractProxyDisplay : AbstractProxyDevice, IProxyDisplay
	{
		public event EventHandler<BoolEventArgs> OnIsPoweredChanged;

		public event DisplayHdmiInputDelegate OnHdmiInputChanged;

		public event EventHandler<ScalingModeEventArgs> OnScalingModeChanged;

		[ApiProperty("IsPowered", "Gets the powered state for the display.")]
		public bool IsPowered { get; private set; }

		[ApiProperty("InputCount", "Gets the HDMI input count for the display.")]
		public int InputCount { get; private set; }

		[ApiProperty("HdmiInput", "Gets the current HDMI input for the display.")]
		public int? HdmiInput { get; private set; }

		[ApiProperty("ScalingMode", "Gets the scaling mode for the display.")]
		public eScalingMode ScalingMode { get; private set; }

		[ApiMethod("PowerOn", "Powers the display.")]
		public void PowerOn()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("PowerOff", "Powers off the display.")]
		public void PowerOff()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("SetHdmiInput", "Sets the HDMI input for the display.")]
		public void SetHdmiInput(int address)
		{
			throw new NotImplementedException();
		}

		[ApiMethod("SetScalingMode", "Sets the scaling mode for the display.")]
		public void SetScalingMode(eScalingMode mode)
		{
			throw new NotImplementedException();
		}
	}
}
