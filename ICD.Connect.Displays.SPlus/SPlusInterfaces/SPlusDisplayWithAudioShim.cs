﻿using System;
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
		public delegate void SPlusDisplayWithAudioInterfaceSetVolumeCallback(ushort raw);

		public delegate void SPlusDisplayWithAudioInterfaceVolumeUpIncrementCallback();

		public delegate void SPlusDisplayWithAudioInterfaceVolumeDownIncrementCallback();

		public delegate void SPlusDisplayWithAudioInterfaceMuteOnCallback();

		public delegate void SPlusDisplayWithAudioInterfaceMuteOffCallback();

		public delegate void SPlusDisplayWithAudioInterfaceMuteToggleCallback();

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

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioInterfaceSetVolumeCallback SetVolumeCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioInterfaceVolumeUpIncrementCallback VolumeUpIncrementCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioInterfaceVolumeDownIncrementCallback VolumeDownIncrementCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioInterfaceMuteOnCallback MuteOnCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioInterfaceMuteOffCallback MuteOffCallback { get; set; }

		[PublicAPI("S+")]
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
			set
			{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return;

				originator.Volume = value;
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
			set
			{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return;

				originator.IsMuted = value.ToBool();
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
			set
			{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return;

				originator.VolumeDeviceMin = value;
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
			set
			{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return;

				originator.VolumeDeviceMax = value;
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
				callback();
		}

		private void OriginatorMuteOffCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceMuteOffCallback callback = MuteOffCallback;
			if (callback != null)
				callback();
		}

		private void OriginatorMuteOnCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceMuteOnCallback callback = MuteOnCallback;
			if (callback != null)
				callback();
		}

		private void OriginatorVolumeDownIncrementCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceVolumeDownIncrementCallback callback = VolumeDownIncrementCallback;
			if (callback != null)
				callback();
		}

		private void OriginatorVolumeUpIncrementCallback(ISimplDisplayWithAudio sender)
		{
			SPlusDisplayWithAudioInterfaceVolumeUpIncrementCallback callback = VolumeUpIncrementCallback;
			if (callback != null)
				callback();
		}

		private void OriginatorSetVolumeCallback(ISimplDisplayWithAudio sender, float volume)
		{
			SPlusDisplayWithAudioInterfaceSetVolumeCallback callback = SetVolumeCallback;
			if (callback != null)
				callback((ushort)volume);
		}

		#endregion
	}
}