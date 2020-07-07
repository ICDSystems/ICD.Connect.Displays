using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Displays.DisplayLift
{
	public sealed class DisplayLiftExternalTelemetryProvider : AbstractExternalTelemetryProvider<IDisplayLiftDevice>
	{
		[EventTelemetry(DisplayLiftTelemetryNames.LIFT_STATE_CHANGED)]
		public event EventHandler<StringEventArgs> OnLiftStateChanged;

		[EventTelemetry(DisplayLiftTelemetryNames.BOOT_DELAY_CHANGED)]
		public event EventHandler<StringEventArgs> OnBootDelayChanged;

		[EventTelemetry(DisplayLiftTelemetryNames.COOL_DELAY_CHANGED)]
		public event EventHandler<StringEventArgs> OnCoolingDelayChanged;

		private string m_LiftState;
		private string m_BootDelay;
		private string m_CoolingDelay;

		[PropertyTelemetry(DisplayLiftTelemetryNames.LIFT_STATE, null, DisplayLiftTelemetryNames.LIFT_STATE_CHANGED)]
		public string LiftState
		{
			get { return m_LiftState; }
			private set
			{
				if (m_LiftState == value)
					return;

				m_LiftState = value;
				OnLiftStateChanged.Raise(this, new StringEventArgs(m_LiftState));
			}
		}

		[PropertyTelemetry(DisplayLiftTelemetryNames.BOOT_DELAY, null, DisplayLiftTelemetryNames.BOOT_DELAY_CHANGED)]
		public string BootDelay
		{
			get { return m_BootDelay; }
			private set
			{
				if (m_LiftState == value)
					return;

				m_BootDelay = value;
				OnBootDelayChanged.Raise(this, new StringEventArgs(m_BootDelay));
			}
		}

		[PropertyTelemetry(DisplayLiftTelemetryNames.COOL_DELAY, null, DisplayLiftTelemetryNames.COOL_DELAY_CHANGED)]
		public string CoolingDelay
		{
			get { return m_CoolingDelay; }
			private set
			{
				if (m_CoolingDelay == value)
					return;

				m_CoolingDelay = value;
				OnCoolingDelayChanged.Raise(this, new StringEventArgs(m_CoolingDelay));
			}
		}

		/// <summary>
		/// Sets the parent telemetry provider that this instance extends.
		/// </summary>
		/// <param name="parent"></param>
		protected override void SetParent(IDisplayLiftDevice parent)
		{
			base.SetParent(parent);

			UpdateValues();
		}

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(IDisplayLiftDevice parent)
		{
			base.Subscribe(parent);

			if (parent == null)
				return;

			parent.OnLiftStateChanged += ParentOnLiftStateChanged;
			parent.OnBootDelayChanged += ParentOnBootDelayChanged;
			parent.OnCoolingDelayChanged += ParentOnCoolingDelayChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(IDisplayLiftDevice parent)
		{
			base.Unsubscribe(parent);

			if (parent == null)
				return;

			parent.OnLiftStateChanged -= ParentOnLiftStateChanged;
			parent.OnBootDelayChanged -= ParentOnBootDelayChanged;
			parent.OnCoolingDelayChanged -= ParentOnCoolingDelayChanged;
		}

		private void ParentOnLiftStateChanged(object sender, EventArgs e)
		{
			UpdateLiftState();
		}

		private void ParentOnBootDelayChanged(object sender, EventArgs e)
		{
			UpdateBootDelay();
		}

		private void ParentOnCoolingDelayChanged(object sender, EventArgs e)
		{
			UpdateCoolingDelay();
		}

		private void UpdateValues()
		{
			UpdateLiftState();
			UpdateBootDelay();
			UpdateCoolingDelay();
		}

		private void UpdateLiftState()
		{
			LiftState = Parent == null ? null : Parent.LiftState.ToString();
		}

		private void UpdateBootDelay()
		{
			BootDelay = Parent == null ? null : Parent.BootDelay + "ms";
		}

		private void UpdateCoolingDelay()
		{
			CoolingDelay = Parent == null ? null : Parent.CoolingDelay + "ms";
		}
	}
}
