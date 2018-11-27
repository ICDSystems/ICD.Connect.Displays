using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.SPlusShims;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.SPlus.Devices.Simpl;

namespace ICD.Connect.Displays.SPlus.SPlusInterfaces
{
	public delegate void SPlusDisplayShimPowerOnCallback();

	public delegate void SPlusDisplayShimPowerOffCallback();

	public delegate void SPlusDisplayShimSetActiveInputCallback(ushort activeInput);

	public delegate void SPlusDisplayShimSetScalingModeCallback(ushort scalingMode);

	public abstract class AbstractSPlusDisplayShim<TOriginator> : AbstractSPlusDeviceShim<TOriginator>
		where TOriginator : class, ISimplDisplay
	{
		#region Events

		/// <summary>
		/// Raised when the power state changes.
		/// </summary>
		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnIsPoweredChanged;

		/// <summary>
		/// Raised when the selected active input changes.
		/// </summary>
		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnActiveInputChanged;

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
		public SPlusDisplayShimSetActiveInputCallback SetActiveInputCallback { get; set; }

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
		/// Gets the active input.
		/// </summary>
		[PublicAPI("S+")]
		public ushort ActiveInput
		{
			get
			{
				TOriginator originator = Originator;
				if (originator == null)
					return 0;

				int? input = originator.ActiveInput;
				return (ushort)(input.HasValue ? input.Value : 0);
			}
			set
			{
				TOriginator originator = Originator;
				if (originator == null)
					return;

				// Input 0 turns into null
				int? input = (value == 0) ? (int?)null : value;

				originator.ActiveInput = input;
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
		/// Sets the active input of the TV, e.g. 1 = HDMI-1.
		/// </summary>
		/// <param name="address"></param>
		[PublicAPI("S+")]
		public void SetActiveInput(ushort address)
		{
			TOriginator originator = Originator;
			if (originator != null)
				originator.SetActiveInput(address);
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

			originator.OnActiveInputChanged += OriginatorOnActiveInputChanged;
			originator.OnIsPoweredChanged += OriginatorOnIsPoweredChanged;
			originator.OnScalingModeChanged += OriginatorOnScalingModeChanged;

			originator.PowerOnCallback = OriginatorPowerOnCallback;
			originator.PowerOffCallback = OriginatorPowerOffCallback;
			originator.SetActiveInputCallback = OriginatorSetActiveInputCallback;
			originator.SetScalingModeCallback = OriginatorSetScalingModeCallback;
		}

		/// <summary>
		/// Unsubscribe from the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Unsubscribe(TOriginator originator)
		{
			base.Unsubscribe(originator);

			if (originator == null)
				return;

			originator.OnActiveInputChanged += OriginatorOnActiveInputChanged;
			originator.OnIsPoweredChanged += OriginatorOnIsPoweredChanged;
			originator.OnScalingModeChanged += OriginatorOnScalingModeChanged;

			originator.PowerOnCallback = null;
			originator.PowerOffCallback = null;
			originator.SetActiveInputCallback = null;
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

		private void OriginatorOnActiveInputChanged(object sender, DisplayInputApiEventArgs displayInputApiEventArgs)
		{
			OnActiveInputChanged.Raise(this, new UShortEventArgs(ActiveInput));
		}

		private void OriginatorSetScalingModeCallback(ISimplDisplay sender, eScalingMode scalingMode)
		{
			SPlusDisplayShimSetScalingModeCallback callback = SetScalingModeCallback;
			if (callback != null)
				callback((ushort)scalingMode);
		}

		private void OriginatorSetActiveInputCallback(ISimplDisplay sender, int address)
		{
			SPlusDisplayShimSetActiveInputCallback callback = SetActiveInputCallback;
			if (callback != null)
				callback((ushort)address);
		}

		private void OriginatorPowerOffCallback(ISimplDisplay sender)
		{
			SPlusDisplayShimPowerOffCallback callback = PowerOffCallback;
			if (callback != null)
				callback();
		}

		private void OriginatorPowerOnCallback(ISimplDisplay sender)
		{
			SPlusDisplayShimPowerOnCallback callback = PowerOnCallback;
			if (callback != null)
				callback();
		}

		#endregion
	}
}
