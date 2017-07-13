using ICD.Common.EventArguments;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Displays
{
	/// <summary>
	/// TODO - Right now this is a simple shim of IDisplayWithAudio. Eventually we should have a specific
	/// volume control for each display type, which contains the actual command building and parsing logic.
	/// </summary>
	public sealed class DisplayVolumeDeviceControl : AbstractVolumeDeviceControl<IDisplayWithAudio>
	{
		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return string.Format("{0} Volume Control", Parent.Name); } }

		/// <summary>
		/// The min volume.
		/// </summary>
		public override float RawVolumeMin { get { return Parent.VolumeDeviceMin; } }

		/// <summary>
		/// The max volume.
		/// </summary>
		public override float RawVolumeMax { get { return Parent.VolumeDeviceMax; } }

		/// <summary>
		/// The volume the control is set to when the device comes online.
		/// </summary>
		public override float? RawVolumeDefault { get { return Parent.VolumeDefault; } set { Parent.VolumeDefault = value; } }

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
		public override void SetRawVolume(float volume)
		{
			Parent.SetVolume(volume);
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetMute(bool mute)
		{
			if (mute)
				Parent.MuteOn();
			else
				Parent.MuteOff();
		}

		/// <summary>
		/// Increments the raw volume once.
		/// </summary>
		public override void RawVolumeIncrement()
		{
			Parent.VolumeUpIncrement();
		}

		/// <summary>
		/// Decrements the raw volume once.
		/// </summary>
		public override void RawVolumeDecrement()
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
			RawVolume = args.Data;
		}

		private void ParentOnMuteStateChanged(object sender, BoolEventArgs args)
		{
			IsMuted = args.Data;
		}

		#endregion
	}
}
