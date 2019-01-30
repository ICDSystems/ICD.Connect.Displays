using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.SPlus.EventArgs;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.Devices.Simpl
{
	public interface ISimplDisplayWithAudio : ISimplDisplay
	{
		[ApiEvent(SPlusDisplayApi.EVENT_SET_VOLUME, SPlusDisplayApi.EVENT_SET_VOLUME_HELP)]
		event EventHandler<SetVolumeApiEventArgs> OnSetVolume;

		[ApiEvent(SPlusDisplayApi.EVENT_SET_VOLUME_INCREMENT, SPlusDisplayApi.EVENT_SET_VOLUME_INCREMENT_HELP)]
		event EventHandler<SetVolumeIncrementApiEventArgs> OnSetVolumeIncrement;

		[ApiEvent(SPlusDisplayApi.EVENT_SET_MUTE, SPlusDisplayApi.EVENT_SET_MUTE_HELP)]
		event EventHandler<SetMuteApiEventArgs> OnSetMute;

		[ApiEvent(SPlusDisplayApi.EVENT_SET_MUTE_TOGGLE, SPlusDisplayApi.EVENT_SET_MUTE_TOGGLE_HELP)]
		event EventHandler<SetMuteToggleApiEventArgs> OnSetMuteToggle;

		[ApiMethod(SPlusDisplayApi.METHOD_SET_VOLUME_FEEDBACK, SPlusDisplayApi.METHOD_SET_VOLUME_FEEDBACK_HELP)]
		void SetVolumeFeedback(float volume);

		[ApiMethod(SPlusDisplayApi.METHOD_SET_MUTE_FEEDBACK, SPlusDisplayApi.METHOD_SET_MUTE_FEEDBACK_HELP)]
		void SetMuteFeedback(bool mute);
	}
}
