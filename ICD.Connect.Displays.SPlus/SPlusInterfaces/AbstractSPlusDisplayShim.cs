using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.SPlusShims;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.SPlus.Devices.Simpl;

namespace ICD.Connect.Displays.SPlus.SPlusInterfaces
{
	public delegate void SPlusDisplayShimPowerOnCallback(object sender);

	public delegate void SPlusDisplayShimPowerOffCallback(object sender);

	public delegate void SPlusDisplayShimSetHdmiInputCallback(object sender, ushort hdmiInput);

	public delegate void SPlusDisplayShimSetScalingModeCallback(object sender, ushort scalingMode);

	public abstract class AbstractSPlusDisplayShim<TOriginator> : AbstractSPlusDeviceShim<TOriginator>
		where TOriginator : ISimplDisplay
	{
		#region Events

		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnIsPoweredChanged;

		/// <summary>
		/// Raised when the selected HDMI input changes.
		/// </summary>
		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnHdmiInputChanged;

		/// <summary>
		/// Raised when the scaling mode changes.
		/// </summary>
		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnScalingModeChanged;

		#endregion

		#region Callbacks

		[PublicAPI("S+")]
		public SPlusDisplayShimPowerOnCallback PowerOnCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayShimPowerOffCallback PowerOffCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayShimSetHdmiInputCallback SetHdmiInputCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayShimSetScalingModeCallback SetScalingModeCallback { get; set; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		[PublicAPI("S+")]
		public ushort IsPowered
		{
			get
			{
				TOriginator originator = Originator;
				if (originator == null)
					return 0;

				return originator.IsPowered.ToUShort();
			}
			set
			{
				TOriginator originator = Originator;
				if (originator == null)
					return;

				originator.IsPowered = value.ToBool();
			}
		}

		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		[PublicAPI("S+")]
		public ushort InputCount
		{
			get
			{
				TOriginator originator = Originator;
				if (originator == null)
					return 0;

				return (ushort)originator.InputCount;
			}
			set
			{
				TOriginator originator = Originator;
				if (originator == null)
					return;

				originator.InputCount = value;
			}
		}

		/// <summary>
		/// Gets the Hdmi input.
		/// </summary>
		[PublicAPI("S+")]
		public ushort HdmiInput
		{
			get
			{
				TOriginator originator = Originator;
				if (originator == null)
					return 0;

				int? input = originator.HdmiInput;
				return (ushort)(input.HasValue ? input.Value : 0);
			}
			set
			{
				TOriginator originator = Originator;
				if (originator == null)
					return;

				originator.HdmiInput = value;
			}
		}

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		[PublicAPI("S+")]
		public ushort ScalingMode
		{
			get
			{
				TOriginator originator = Originator;
				if (originator == null)
					return 0;

				return (ushort)originator.ScalingMode;
			}
			set
			{
				TOriginator originator = Originator;
				if (originator == null)
					return;

				originator.ScalingMode = (eScalingMode)value;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Powers the TV.
		/// </summary>
		[PublicAPI("S+")]
		public void PowerOn()
		{
			TOriginator originator = Originator;
			if (originator != null)
				originator.PowerOn();
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		[PublicAPI("S+")]
		public void PowerOff()
		{
			TOriginator originator = Originator;
			if (originator != null)
				originator.PowerOff();
		}

		/// <summary>
		/// Sets the Hdmi index of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		[PublicAPI("S+")]
		public void SetHdmiInput(ushort address)
		{
			TOriginator originator = Originator;
			if (originator != null)
				originator.SetHdmiInput(address);
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		[PublicAPI("S+")]
		public void SetScalingMode(ushort mode)
		{
			TOriginator originator = Originator;
			if (originator != null)
				originator.SetScalingMode((eScalingMode)mode);
		}

		#endregion

		#region Originator Callbacks

		/// <summary>
		/// Subscribes to the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Subscribe(TOriginator originator)
		{
			base.Subscribe(originator);

			if (originator == null)
				return;

			originator.OnHdmiInputChanged += OriginatorOnHdmiInputChanged;
			originator.OnIsPoweredChanged += OriginatorOnIsPoweredChanged;
			originator.OnScalingModeChanged += OriginatorOnScalingModeChanged;

			originator.PowerOnCallback = OriginatorPowerOnCallback;
			originator.PowerOffCallback = OriginatorPowerOffCallback;
			originator.SetHdmiInputCallback = OriginatorSetHdmiInputCallback;
			originator.SetScalingModeCallback = OriginatorSetScalingModeCallback;
		}

		/// <summary>
		/// Unsubscribe from the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Unsubscribe(TOriginator originator)
		{
			base.Unsubscribe(originator);

			originator.OnHdmiInputChanged += OriginatorOnHdmiInputChanged;
			originator.OnIsPoweredChanged += OriginatorOnIsPoweredChanged;
			originator.OnScalingModeChanged += OriginatorOnScalingModeChanged;

			originator.PowerOnCallback = null;
			originator.PowerOffCallback = null;
			originator.SetHdmiInputCallback = null;
			originator.SetScalingModeCallback = null;
		}

		private void OriginatorOnScalingModeChanged(object sender, DisplayScalingModeApiEventArgs displayScalingModeApiEventArgs)
		{
			OnScalingModeChanged.Raise(this, new UShortEventArgs(ScalingMode));
		}

		private void OriginatorOnIsPoweredChanged(object sender, DisplayPowerStateApiEventArgs displayPowerStateApiEventArgs)
		{
			OnIsPoweredChanged.Raise(this, new UShortEventArgs(IsPowered));
		}

		private void OriginatorOnHdmiInputChanged(object sender, DisplayHmdiInputApiEventArgs displayHmdiInputApiEventArgs)
		{
			OnHdmiInputChanged.Raise(this, new UShortEventArgs(HdmiInput));
		}

		private void OriginatorSetScalingModeCallback(ISimplDisplay sender, eScalingMode scalingMode)
		{
			SPlusDisplayShimSetScalingModeCallback handler = SetScalingModeCallback;
			if (handler != null)
				handler(this, (ushort)scalingMode);
		}

		private void OriginatorSetHdmiInputCallback(ISimplDisplay sender, int address)
		{
			SPlusDisplayShimSetHdmiInputCallback handler = SetHdmiInputCallback;
			if (handler != null)
				handler(this, (ushort)address);
		}

		private void OriginatorPowerOffCallback(ISimplDisplay sender)
		{
			SPlusDisplayShimPowerOffCallback handler = PowerOffCallback;
			if (handler != null)
				handler(this);
		}

		private void OriginatorPowerOnCallback(ISimplDisplay sender)
		{
			SPlusDisplayShimPowerOnCallback handler = PowerOnCallback;
			if (handler != null)
				handler(this);
		}

		#endregion
	}
}
