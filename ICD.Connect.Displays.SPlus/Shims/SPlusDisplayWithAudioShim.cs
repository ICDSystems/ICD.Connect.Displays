using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Displays.SPlus.Devices.Simpl;
using ICD.Connect.Displays.SPlus.EventArgs;

namespace ICD.Connect.Displays.SPlus.Shims
{
	[PublicAPI("S+")]
	public sealed class SPlusDisplayWithAudioShim : AbstractSPlusDisplayShim<ISimplDisplayWithAudio>
	{
		public delegate void SPlusDisplayWithAudioUshortCallback(ushort raw);

		public delegate void SPlusDisplayWithAudioCallback();

		#region Callbacks

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioUshortCallback SetVolumeCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioCallback VolumeUpIncrementCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioCallback VolumeDownIncrementCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioCallback MuteOnCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioCallback MuteOffCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayWithAudioCallback MuteToggleCallback { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the current volume.
		/// </summary>
		[PublicAPI("S+")]
		public void SetVolumeFeedback(ushort volume)
		{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return;

				originator.SetVolumeFeedback(volume);
			
		}

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[PublicAPI("S+")]
		public void SetMuteFeedback(ushort mute)
		{
				ISimplDisplayWithAudio originator = Originator;
				if (originator == null)
					return;

				originator.SetMuteFeedback(mute.ToBool());
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

			originator.OnSetVolume += OriginatorOnSetVolume;
			originator.OnSetVolumeIncrement += OriginatorOnSetVolumeIncrement;
			originator.OnSetMute += OriginatorOnSetMute;
			originator.OnSetMuteToggle += OriginatorOnSetMuteToggle;
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

			originator.OnSetVolume -= OriginatorOnSetVolume;
			originator.OnSetVolumeIncrement -= OriginatorOnSetVolumeIncrement;
			originator.OnSetMute -= OriginatorOnSetMute;
			originator.OnSetMuteToggle -= OriginatorOnSetMuteToggle;

			
		}

		private void OriginatorOnSetVolume(object sender, SetVolumeApiEventArgs args)
		{
			SPlusDisplayWithAudioUshortCallback callback = SetVolumeCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}

		private void OriginatorOnSetVolumeIncrement(object sender, SetVolumeIncrementApiEventArgs args)
		{
			SPlusDisplayWithAudioCallback callback = args.Data ? VolumeUpIncrementCallback : VolumeDownIncrementCallback;
			if (callback != null)
				callback();
		}

		private void OriginatorOnSetMute(object sender, SetMuteApiEventArgs args)
		{
			SPlusDisplayWithAudioCallback callback = args.Data ? MuteOnCallback : MuteOffCallback;

			if (callback != null)
				callback();
		}

		private void OriginatorOnSetMuteToggle(object sender, SetMuteToggleApiEventArgs args)
		{
			SPlusDisplayWithAudioCallback callback = MuteToggleCallback;
			if (callback != null)
				callback();
		}

		#endregion
	}
}
