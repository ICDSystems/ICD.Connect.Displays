using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Displays
{
	/// <summary>
	/// TODO - Right now this is a simple shim of IDisplayWithAudio. Eventually we should have a specific
	/// volume control for each display type, which contains the actual command building and parsing logic.
	/// </summary>
	public sealed class DisplayVolumeDeviceControl : AbstractVolumeRawLevelDeviceControl<IDisplayWithAudio>, IVolumeMuteFeedbackDeviceControl
	{
		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return string.Format("{0} Volume Control", Parent.Name); } }

		/// <summary>
		/// The min volume.
		/// </summary>
		protected override float VolumeRawMinAbsolute { get { return Parent.VolumeDeviceMin; } }

		/// <summary>
		/// The max volume.
		/// </summary>
		protected override float VolumeRawMaxAbsolute { get { return Parent.VolumeDeviceMax; } }

		/// <summary>
		/// Safety Min Volume Set on the device
		/// </summary>
		public override float? VolumeRawMin { get { return Parent.VolumeSafetyMin; }}

		/// <summary>
		/// Safety Max Volume Set on the device
		/// </summary>
		public override float? VolumeRawMax { get { return Parent.VolumeSafetyMax; }}

		public override float VolumeRaw
		{
			get { return Parent.Volume; }
		}

		public bool VolumeIsMuted
		{
			get { return Parent.IsMuted; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public DisplayVolumeDeviceControl(IDisplayWithAudio parent, int id)
			: base(parent, id)
		{
			Subscribe(parent);
		}

		#region Events

		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		#endregion

		#region Methods

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(Parent);
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public override void SetVolumeRaw(float volume)
		{
			Parent.SetVolume(volume);
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			if (mute)
				Parent.MuteOn();
			else
				Parent.MuteOff();
		}

		public void VolumeMuteToggle()
		{
			Parent.MuteToggle();
		}

		/// <summary>
		/// Increments the raw volume once.
		/// </summary>
		public override void VolumeLevelIncrement()
		{
			Parent.VolumeUpIncrement();
		}

		/// <summary>
		/// Decrements the raw volume once.
		/// </summary>
		public override void VolumeLevelDecrement()
		{
			Parent.VolumeDownIncrement();
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(IDisplayWithAudio parent)
		{
			parent.OnVolumeChanged += ParentOnVolumeChanged;
			parent.OnMuteStateChanged += ParentOnMuteStateChanged;
		}

		private void Unsubscribe(IDisplayWithAudio parent)
		{
			parent.OnVolumeChanged -= ParentOnVolumeChanged;
			parent.OnMuteStateChanged -= ParentOnMuteStateChanged;
		}

		private void ParentOnVolumeChanged(object sender, FloatEventArgs args)
		{
			IPowerDeviceControl senderAsPowerControl = sender as IPowerDeviceControl;
			if (senderAsPowerControl != null && !senderAsPowerControl.IsPowered)
				return;

			VolumeFeedback(args.Data);
		}

		private void ParentOnMuteStateChanged(object sender, BoolEventArgs args)
		{
			OnMuteStateChanged.Raise(this, args);
		}

		#endregion
	}
}
