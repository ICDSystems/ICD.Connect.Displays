using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Samsung;
using ICD.Connect.Displays.Sharp;
using ICD.Connect.Protocol.Ports;

namespace ICD.Connect.Displays.SPlus
{
	[PublicAPI]
	public sealed class SPlusDisplayInterface : IDisposable
	{
		public delegate void DelVolumeChanged(ushort volume, ushort volumeRaw);

		public delegate void DelIsPoweredChanged(ushort powered);

		public delegate void DelHdmiInputChanged(ushort input);

		public delegate void DelIsMutedChanged(ushort muted);

		public delegate void DelScalingModeChanged(ushort mode);

		[PublicAPI] public const ushort SHARP_DISPLAY = 0;
		[PublicAPI] public const ushort SAMSUNG_DISPLAY = 1;
		[PublicAPI] public const ushort SAMSUNG_PRO_DISPLAY = 2;

		private IDisplayWithAudio m_Display;

		#region Properties

		[PublicAPI]
		public DelVolumeChanged VolumeChanged { get; set; }

		[PublicAPI]
		public DelIsPoweredChanged PoweredChanged { get; set; }

		[PublicAPI]
		public DelHdmiInputChanged HdmiInputChanged { get; set; }

		[PublicAPI]
		public DelIsMutedChanged MutedChanged { get; set; }

		[PublicAPI]
		public DelScalingModeChanged ScalingModeChanged { get; set; }

		/// <summary>
		/// Gets the volume in the range 0-100.
		/// </summary>
		[PublicAPI]
		public ushort Volume { get { return (ushort)(m_Display.GetVolumeAsPercentage() * 100); } }

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		[PublicAPI]
		public ushort IsPowered { get { return m_Display.IsPowered ? (ushort)1 : (ushort)0; } }

		/// <summary>
		/// Gets the Hdmi input index.
		/// </summary>
		[PublicAPI]
		public ushort HdmiInput { get { return (ushort)(m_Display.HdmiInput ?? 0); } }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		[PublicAPI]
		public ushort IsMuted { get { return m_Display.IsMuted ? (ushort)1 : (ushort)0; } }

		/// <summary>
		/// Gets the scaling mode.
		/// </summary>
		[PublicAPI]
		public ushort ScalingMode { get { return (ushort)m_Display.ScalingMode; } }

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the display interface to start accepting calls.
		/// </summary>
		/// <param name="displayId"></param>
		/// <param name="port"></param>
		[PublicAPI]
		public void Initialize(ushort displayId, object port)
		{
			Unsubscribe(m_Display);
			m_Display = InstantiateDisplay(displayId, port as ISerialPort);
			Subscribe(m_Display);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_Display);
		}

		/// <summary>
		/// Powers the TV.
		/// </summary>
		[PublicAPI]
		public void PowerOn()
		{
			m_Display.PowerOn();
		}

		/// <summary>
		/// Shuts down the TV.
		/// </summary>
		[PublicAPI]
		public void PowerOff()
		{
			m_Display.PowerOff();
		}

		[PublicAPI]
		public void MuteOn()
		{
			m_Display.MuteOn();
		}

		[PublicAPI]
		public void MuteOff()
		{
			m_Display.MuteOff();
		}

		[PublicAPI]
		public void MuteToggle()
		{
			m_Display.MuteToggle();
		}

		[PublicAPI]
		public void VolumeSetAsPercentage(ushort volume)
		{
			float percentage = MathUtils.MapRange(0.0f, ushort.MaxValue, 0.0f, 1.0f, volume);
			m_Display.SetVolumeAsPercentage(percentage);
		}

		[PublicAPI]
		public void VolumeSetRaw(ushort raw)
		{
			m_Display.SetVolume(raw);
		}

		[PublicAPI]
		public void VolumeUpIncrement()
		{
			m_Display.VolumeUpIncrement();
		}

		[PublicAPI]
		public void VolumeDownIncrement()
		{
			m_Display.VolumeDownIncrement();
		}

		[PublicAPI]
		public void SetHdmiInput(ushort index)
		{
			m_Display.SetHdmiInput(index);
		}

		[PublicAPI]
		public void SetScalingMode(ushort mode)
		{
			m_Display.SetScalingMode((eScalingMode)mode);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Subscribe to the display events.
		/// </summary>
		/// <param name="display"></param>
		private void Subscribe(IDisplayWithAudio display)
		{
			if (display == null)
				return;

			display.OnVolumeChanged += DisplayOnVolumeChanged;
			display.OnHdmiInputChanged += DisplayOnHdmiInputChanged;
			display.OnMuteStateChanged += DisplayOnIsMutedChanged;
			display.OnIsPoweredChanged += DisplayOnIsPoweredChanged;
			display.OnScalingModeChanged += DisplayOnScalingModeChanged;
		}

		/// <summary>
		/// Unsubscribe from the display events.
		/// </summary>
		/// <param name="display"></param>
		private void Unsubscribe(IDisplayWithAudio display)
		{
			if (display == null)
				return;

			display.OnVolumeChanged -= DisplayOnVolumeChanged;
			display.OnHdmiInputChanged -= DisplayOnHdmiInputChanged;
			display.OnMuteStateChanged -= DisplayOnIsMutedChanged;
			display.OnIsPoweredChanged -= DisplayOnIsPoweredChanged;
			display.OnScalingModeChanged -= DisplayOnScalingModeChanged;
		}

		private void DisplayOnVolumeChanged(object sender, DisplayVolumeApiEventArgs args)
		{
			if (VolumeChanged == null)
				return;

			ushort volume = (ushort)args.Data;
			ushort percentage = (ushort)(m_Display.GetVolumeAsPercentage() * ushort.MaxValue);

			VolumeChanged(percentage, volume);
		}

		private void DisplayOnScalingModeChanged(object sender, DisplayScalingModeApiEventArgs args)
		{
			if (ScalingModeChanged != null)
				ScalingModeChanged((ushort)args.Data);
		}

		private void DisplayOnIsPoweredChanged(object sender, DisplayPowerStateApiEventArgs args)
		{
			if (PoweredChanged != null)
				PoweredChanged(args.Data ? (ushort)1 : (ushort)0);
		}

		private void DisplayOnIsMutedChanged(object sender, DisplayMuteApiEventArgs args)
		{
			if (MutedChanged != null)
				MutedChanged(args.Data ? (ushort)1 : (ushort)0);
		}

		private void DisplayOnHdmiInputChanged(object sender, DisplayHmdiInputApiEventArgs args)
		{
			if (HdmiInputChanged != null)
				HdmiInputChanged((ushort)args.HdmiInput);
		}

		/// <summary>
		/// Creates a new display instance.
		/// </summary>
		/// <param name="displayId"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		private static IDisplayWithAudio InstantiateDisplay(ushort displayId, ISerialPort port)
		{
			switch (displayId)
			{
				case SAMSUNG_DISPLAY:
					SamsungDisplay samsung = new SamsungDisplay();
					samsung.SetPort(port);
					return samsung;

				case SAMSUNG_PRO_DISPLAY:
					SamsungProDisplay samsungPro = new SamsungProDisplay();
					samsungPro.SetPort(port);
					return samsungPro;

				case SHARP_DISPLAY:
					SharpDisplay sharp = new SharpDisplay();
					sharp.SetPort(port);
					return sharp;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion
	}
}
