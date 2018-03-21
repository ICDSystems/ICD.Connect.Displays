using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Protocol.EventArguments;

namespace ICD.Connect.Displays.Mock
{
	/// <summary>
	/// Mock display device for testing control systems.
	/// TODO - Currently we're inheriting from some device that communicates over serial. Break this up.
	/// </summary>
	public sealed class MockDisplayWithAudio : AbstractDisplayWithAudio<MockDisplayWithAudioSettings>
	{
		/// <summary>
		/// Gets the number of HDMI inputs.
		/// </summary>
		public override int InputCount { get { return 1; } }

		#region Methods

		/// <summary>
		/// Sets IsPowered to true.
		/// </summary>
		public override void PowerOn()
		{
			IsPowered = true;
		}

		/// <summary>
		/// Sets IsPowered to false.
		/// </summary>
		public override void PowerOff()
		{
			IsPowered = false;
		}

		/// <summary>
		/// Sets the HDMI input.
		/// </summary>
		/// <param name="index"></param>
		public override void SetHdmiInput(int index)
		{
			HdmiInput = index;
		}

		/// <summary>
		/// Sets the scaling mode.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetScalingMode(eScalingMode mode)
		{
			ScalingMode = mode;
		}

		/// <summary>
		/// Enables mute.
		/// </summary>
		public override void MuteOn()
		{
			IsMuted = true;
		}

		/// <summary>
		/// Disables mute.
		/// </summary>
		public override void MuteOff()
		{
			IsMuted = false;
		}

		/// <summary>
		/// Increments volume.
		/// </summary>
		public override void VolumeUpIncrement()
		{
            if (!IsPowered)
                return;
			if (Volume < 100)
				Volume++;
		}

		/// <summary>
		/// Decrements volume.
		/// </summary>
		public override void VolumeDownIncrement()
        {
            if (!IsPowered)
                return;
			if (Volume > 0)
				Volume--;
		}

		#endregion

		#region Private Methods

		protected override void VolumeSetRawFinal(float raw)
		{
            if (!IsPowered)
                return;
			Volume = raw;
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		/// <summary>
		/// Called when a command gets a response from the physical display.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
		}

		/// <summary>
		/// Called when a command times out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected override void SerialQueueOnTimeout(object sender, SerialDataEventArgs args)
		{
		}

		#endregion
	}
}
