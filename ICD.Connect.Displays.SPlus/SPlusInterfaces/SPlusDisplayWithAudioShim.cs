using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.SPlus.Devices.Simpl;

namespace ICD.Connect.Displays.SPlus.SPlusInterfaces
{
	[PublicAPI("S+")]
	public sealed class SPlusDisplayWithAudioShim : AbstractSPlusDisplayShim<ISimplDisplayWithAudio>
	{
		public delegate void SPlusDisplayWithAudioInterfaceSetVolumeCallback(object sender, ushort raw);

		public delegate void SPlusDisplayWithAudioInterfaceVolumeUpIncrementCallback(object sender);

		public delegate void SPlusDisplayWithAudioInterfaceVolumeDownIncrementCallback(object sender);

		public delegate void SPlusDisplayWithAudioInterfaceMuteOnCallback(object sender);

		public delegate void SPlusDisplayWithAudioInterfaceMuteOffCallback(object sender);

		public delegate void SPlusDisplayWithAudioInterfaceMuteToggleCallback(object sender);

		/// <summary>
		/// Raised when the volume changes.
		/// </summary>
		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnVolumeChanged;

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		[PublicAPI("S+")]
		public event EventHandler<UShortEventArgs> OnMuteStateChanged;

		#region Callbacks

		public SPlusDisplayWithAudioInterfaceSetVolumeCallback SetVolumeCallback { get; set; }

		public SPlusDisplayWithAudioInterfaceVolumeUpIncrementCallback VolumeUpIncrementCallback { get; set; }

		public SPlusDisplayWithAudioInterfaceVolumeDownIncrementCallback VolumeDownIncrementCallback { get; set; }

		public SPlusDisplayWithAudioInterfaceMuteOnCallback MuteOnCallback { get; set; }

		public SPlusDisplayWithAudioInterfaceMuteOffCallback MuteOffCallback { get; set; }

		public SPlusDisplayWithAudioInterfaceMuteToggleCallback MuteToggleCallback { get; set; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the current volume.
		/// </summary>
		[PublicAPI("S+")]
		public ushort Volume
		{
			get
			{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return 0;

				return (ushort)originator.Volume;
			}
		}

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[PublicAPI("S+")]
		public ushort IsMuted
		{
			get
			{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return 0;

				return (ushort)(originator.IsMuted ? 1 : 0);
			}
		}

		/// <summary>
		/// The min volume.
		/// </summary>
		[PublicAPI("S+")]
		public ushort VolumeDeviceMin
		{
			get
			{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return 0;

				return (ushort)originator.VolumeDeviceMin;
			}
		}

		/// <summary>
		/// The max volume.
		/// </summary>
		[PublicAPI("S+")]
		public ushort VolumeDeviceMax
		{
			get
			{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return 0;

				return (ushort)originator.VolumeDeviceMax;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the current volume.
		/// </summary>
		/// <param name="raw"></param>
		[PublicAPI("S+")]
		public void SetVolume(ushort raw)
		{
			ISimplDisplayWithAudio originator = Originator;
			if (originator != null)
				originator.SetVolume(raw);
		}

		/// <summary>
		/// Increments the current volume.
		/// </summary>
		[PublicAPI("S+")]
		public void VolumeUpIncrement()
		{
			ISimplDisplayWithAudio originator = Originator;
			if (originator != null)
				originator.VolumeUpIncrement();
		}

		/// <summary>
		/// Decrements the current volume.
		/// </summary>
		[PublicAPI("S+")]
		public void VolumeDownIncrement()
		{
			ISimplDisplayWithAudio originator = Originator;
			if (originator != null)
				originator.VolumeDownIncrement();
		}

		/// <summary>
		/// Mutes the display.
		/// </summary>
		[PublicAPI("S+")]
		public void MuteOn()
		{
			ISimplDisplayWithAudio originator = Originator;
			if (originator != null)
				originator.MuteOn();
		}

		/// <summary>
		/// Unmutes the display.
		/// </summary>
		[PublicAPI("S+")]
		public void MuteOff()
		{
			ISimplDisplayWithAudio originator = Originator;
			if (originator != null)
				originator.MuteOff();
		}

		/// <summary>
		/// Toggles the current mute state on the display.
		/// </summary>
		[PublicAPI("S+")]
		public void MuteToggle()
		{
			ISimplDisplayWithAudio originator = Originator;
			if (originator != null)
				originator.MuteToggle();
		}

		#endregion

		#region Originator Callbacks

		/// <summary>
		/// Subscribes to the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Subscribe(ISimplDisplayWithAudio originator)
		{
			base.Subscribe(originator);

			if (originator == null)
				return;

			originator.OnVolumeChanged += OriginatorOnVolumeChanged;
			originator.OnMuteStateChanged += OriginatorOnMuteStateChanged;

			originator.SetVolumeCallback = OriginatorSetVolumeCallback;
			originator.VolumeUpIncrementCallback = OriginatorVolumeUpIncrementCallback;
			originator.VolumeDownIncrementCallback = OriginatorVolumeDownIncrementCallback;
			originator.MuteOnCallback = OriginatorMuteOnCallback;
			originator.MuteOffCallback = OriginatorMuteOffCallback;
			originator.MuteToggleCallback = OriginatorMuteToggleCallback;
		}

		/// <summary>
		/// Unsubscribe from the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Unsubscribe(ISimplDisplayWithAudio originator)
		{
			base.Unsubscribe(originator);

			if (originator == null)
				return;

			originator.OnVolumeChanged -= OriginatorOnVolumeChanged;
			originator.OnMuteStateChanged -= OriginatorOnMuteStateChanged;

			originator.SetVolumeCallback = null;
			originator.VolumeUpIncrementCallback = null;
			originator.VolumeDownIncrementCallback = null;
			originator.MuteOnCallback = null;
			originator.MuteOffCallback = null;
			originator.MuteToggleCallback = null;
		}

		private void OriginatorOnMuteStateChanged(object sender, DisplayMuteApiEventArgs displayMuteApiEventArgs)
		{
			OnMuteStateChanged.Raise(this, new UShortEventArgs(IsMuted));
		}

		private void OriginatorOnVolumeChanged(object sender, DisplayVolumeApiEventArgs displayVolumeApiEventArgs)
		{
			OnMuteStateChanged.Raise(this, new UShortEventArgs(Volume));
		}

		private void OriginatorMuteToggleCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceMuteToggleCallback callback = MuteToggleCallback;
			if (callback != null)
				callback(this);
		}

		private void OriginatorMuteOffCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceMuteOffCallback callback = MuteOffCallback;
			if (callback != null)
				callback(this);
		}

		private void OriginatorMuteOnCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceMuteToggleCallback callback = MuteToggleCallback;
			if (callback != null)
				callback(this);
		}

		private void OriginatorVolumeDownIncrementCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceVolumeDownIncrementCallback callback = VolumeDownIncrementCallback;
			if (callback != null)
				callback(this);
		}

		private void OriginatorVolumeUpIncrementCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceVolumeUpIncrementCallback callback = VolumeUpIncrementCallback;
			if (callback != null)
				callback(this);
		}

		private void OriginatorSetVolumeCallback(ISimplDisplayWithAudio sender, float volume)
		{
			SPlusDisplayWithAudioInterfaceSetVolumeCallback callback = SetVolumeCallback;
			if (callback != null)
				callback(this, (ushort)volume);
		}

		#endregion
	}
}
