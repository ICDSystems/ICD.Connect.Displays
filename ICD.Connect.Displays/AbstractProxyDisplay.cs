using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays
{
	public abstract class AbstractProxyDisplay : AbstractProxyDevice, IProxyDisplay
	{
		public event EventHandler<BoolEventArgs> OnIsPoweredChanged;

		public event DisplayHdmiInputDelegate OnHdmiInputChanged;

		public event EventHandler<ScalingModeEventArgs> OnScalingModeChanged;

		public bool IsPowered { get; }

		public int InputCount { get; }

		public int? HdmiInput { get; }

		public eScalingMode ScalingMode { get; }

		public void PowerOn()
		{
			throw new NotImplementedException();
		}

		public void PowerOff()
		{
			throw new NotImplementedException();
		}

		public void SetHdmiInput(int address)
		{
			throw new NotImplementedException();
		}

		public void SetScalingMode(eScalingMode mode)
		{
			throw new NotImplementedException();
		}
	}
}
