using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API;
using ICD.Connect.API.Info;
using ICD.Connect.Displays.SPlus.Devices.Simpl;
using ICD.Connect.Displays.SPlus.EventArgs;

namespace ICD.Connect.Displays.SPlus.Proxy
{
	public sealed class ProxySimplDisplayWithAudio : AbstractProxySimplDisplay<ProxySimplDisplayWithAudioSettings>, ISimplDisplayWithAudio
	{
		public event EventHandler<SetVolumeApiEventArgs> OnSetVolume;
		public event EventHandler<SetVolumeIncrementApiEventArgs> OnSetVolumeIncrement;
		public event EventHandler<SetMuteApiEventArgs> OnSetMute;
		public event EventHandler<SetMuteToggleApiEventArgs> OnSetMuteToggle;

		#region Methods

		public void SetVolumeFeedback(float volume)
		{
			CallMethod(SPlusDisplayApi.METHOD_SET_VOLUME_FEEDBACK, volume);
		}

		public void SetMuteFeedback(bool mute)
		{
			CallMethod(SPlusDisplayApi.METHOD_SET_MUTE_FEEDBACK, mute);
		}

		#endregion

		#region API

		/// <summary>
		/// Override to build initialization commands on top of the current class info.
		/// </summary>
		/// <param name="command"></param>
		protected override void Initialize(ApiClassInfo command)
		{
			base.Initialize(command);

			ApiCommandBuilder.UpdateCommand(command)
							 .SubscribeEvent(SPlusDisplayApi.EVENT_SET_VOLUME)
							 .SubscribeEvent(SPlusDisplayApi.EVENT_SET_VOLUME_INCREMENT)
							 .SubscribeEvent(SPlusDisplayApi.EVENT_SET_MUTE)
							 .SubscribeEvent(SPlusDisplayApi.EVENT_SET_MUTE_TOGGLE)
							 .Complete();
		}

		/// <summary>
		/// Updates the proxy with event feedback info.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		protected override void ParseEvent(string name, ApiResult result)
		{
			base.ParseEvent(name, result);

			switch (name)
			{
				case SPlusDisplayApi.EVENT_SET_VOLUME:
					float volume = result.GetValue<float>();
					RaiseSetVolume(volume);
					break;
				case SPlusDisplayApi.EVENT_SET_VOLUME_INCREMENT:
					bool direction = result.GetValue<bool>();
					RaiseSetVolumeIncrement(direction);
					break;
				case SPlusDisplayApi.EVENT_SET_MUTE:
					bool mute = result.GetValue<bool>();
					RaiseSetMute(mute);
					break;
				case SPlusDisplayApi.EVENT_SET_MUTE_TOGGLE:
					RaiseSetMuteToggle();
					break;
			}
		}

		#endregion

		#region Private Methods

		private void RaiseSetVolume(float volume)
		{
			OnSetVolume.Raise(this, new SetVolumeApiEventArgs(volume));
		}

		private void RaiseSetVolumeIncrement(bool direction)
		{
			OnSetVolumeIncrement.Raise(this, new SetVolumeIncrementApiEventArgs(direction));
		}

		private void RaiseSetMute(bool mute)
		{
			OnSetMute.Raise(this, new SetMuteApiEventArgs(mute));
		}

		private void RaiseSetMuteToggle()
		{
			OnSetMuteToggle.Raise(this, new SetMuteToggleApiEventArgs());
		}

		#endregion
	}
}