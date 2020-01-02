using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Devices
{
	public sealed class DisplayVolumeDeviceControl : AbstractVolumeDeviceControl<IDisplayWithAudio>
	{
		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return string.Format("{0} Volume Control", Parent.Name); } }

		/// <summary>
		/// Returns the features that are supported by this volume control.
		/// </summary>
		public override eVolumeFeatures SupportedVolumeFeatures { get { return Parent.SupportedVolumeFeatures; } }

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return Parent.VolumeDeviceMin; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return Parent.VolumeDeviceMax; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public DisplayVolumeDeviceControl(IDisplayWithAudio parent, int id)
			: base(parent, id)
		{
		}

		#region Methods

		/// <summary>
		/// Sets the raw volume.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			Parent.SetVolume(level);
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			if (mute)
				Parent.MuteOn();
			else
				Parent.MuteOff();
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			Parent.MuteToggle();
		}

		/// <summary>
		/// Increments the raw volume once.
		/// </summary>
		public override void VolumeIncrement()
		{
			Parent.VolumeUpIncrement();
		}

		/// <summary>
		/// Decrements the raw volume once.
		/// </summary>
		public override void VolumeDecrement()
		{
			Parent.VolumeDownIncrement();
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public override void VolumeRamp(bool increment, long timeout)
		{
			Parent.VolumeRamp(increment, timeout);
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public override void VolumeRampStop()
		{
			Parent.VolumeRampStop();
		}

		protected override bool GetControlAvailable()
		{
			return Parent.VolumeControlAvailable;
		}

		#endregion

		#region Parent Callbacks

		protected override void Subscribe(IDisplayWithAudio parent)
		{
			base.Subscribe(parent);

			parent.OnVolumeChanged += ParentOnVolumeChanged;
			parent.OnMuteStateChanged += ParentOnMuteStateChanged;
			parent.OnVolumeControlAvailableChanged += ParentOnVolumeControlAvailableChanged;
		}

		protected override void Unsubscribe(IDisplayWithAudio parent)
		{
			base.Unsubscribe(parent);

			parent.OnVolumeChanged -= ParentOnVolumeChanged;
			parent.OnMuteStateChanged -= ParentOnMuteStateChanged;
			parent.OnVolumeControlAvailableChanged -= ParentOnVolumeControlAvailableChanged;
		}

		private void ParentOnVolumeChanged(object sender, DisplayVolumeApiEventArgs args)
		{
			IPowerDeviceControl senderAsPowerControl = sender as IPowerDeviceControl;
			if (senderAsPowerControl != null && senderAsPowerControl.PowerState == ePowerState.PowerOff)
				return;

			VolumeLevel = args.Data;
		}

		private void ParentOnMuteStateChanged(object sender, DisplayMuteApiEventArgs args)
		{
			IsMuted = args.Data;
		}

		private void ParentOnVolumeControlAvailableChanged(object sender, DisplayVolumeControlAvailableApiEventArgs e)
		{
			UpdateCachedControlAvailable();
		}

		#endregion
	}
}
