using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Telemetry;

namespace ICD.Connect.Displays.Devices
{
	public sealed class DisplayWithAudioExternalTelemetryProvider : IDisplayWithAudioExternalTelemetryProvider
	{
		/// <summary>
		/// Raised when the volume percent changes.
		/// </summary>
		public event EventHandler<DisplayVolumeApiEventArgs> OnVolumePercentChanged;

		private float m_VolumePercent;
		private IDisplayWithAudio m_Parent;

		/// <summary>
		/// Gets the volume as a float represented from 0.0f (silent) to 1.0f (as loud as possible)
		/// </summary>
		public float VolumePercent
		{
			get { return m_VolumePercent; }
			private set
			{
				if (Math.Abs(value - m_VolumePercent) < 0.001f)
					return;

				m_VolumePercent = value;

				OnVolumePercentChanged.Raise(this, new DisplayVolumeApiEventArgs(m_VolumePercent));
			}
		}

		/// <summary>
		/// Sets the parent telemetry provider that this instance extends.
		/// </summary>
		/// <param name="provider"></param>
		public void SetParent(ITelemetryProvider provider)
		{
			Unsubscribe(m_Parent);
			m_Parent = provider as IDisplayWithAudio;
			Subscribe(m_Parent);

			UpdateValues();
		}

		private void Subscribe(IDisplayWithAudio parent)
		{
			if (parent == null)
				return;

			parent.OnVolumeChanged += ParentOnVolumeChanged;
		}

		private void Unsubscribe(IDisplayWithAudio parent)
		{
			if (parent == null)
				return;

			parent.OnVolumeChanged -= ParentOnVolumeChanged;
		}

		private void ParentOnVolumeChanged(object sender, EventArgs e)
		{
			UpdateVolumePercent();
		}

		private void UpdateValues()
		{
			UpdateVolumePercent();
		}

		private void UpdateVolumePercent()
		{
			VolumePercent = m_Parent.GetVolumeAsPercentage();
		}
	}
}