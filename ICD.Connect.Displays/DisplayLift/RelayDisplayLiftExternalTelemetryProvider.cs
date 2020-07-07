using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Displays.DisplayLift
{
	public sealed class RelayDisplayLiftExternalTelemetryProvider : AbstractExternalTelemetryProvider<RelayDisplayLiftDevice>
	{
		[EventTelemetry(DisplayLiftTelemetryNames.LATCH_MODE_CHANGED)]
		public event EventHandler<BoolEventArgs> OnLatchModeChanged;

		[EventTelemetry(DisplayLiftTelemetryNames.EXTEND_TIME_CHANGED)]
		public event EventHandler<StringEventArgs> OnExtendTimeChanged;

		[EventTelemetry(DisplayLiftTelemetryNames.RETRACT_TIME_CHANGED)]
		public event EventHandler<StringEventArgs> OnRetractTimeChanged;

		private bool m_LatchMode;
		private string m_ExtendTime;
		private string m_RetractTime;

		[PropertyTelemetry(DisplayLiftTelemetryNames.LATCH_MODE, null, DisplayLiftTelemetryNames.LATCH_MODE_CHANGED)]
		public bool LatchMode
		{
			get { return m_LatchMode; }
			private set
			{
				if (m_LatchMode == value)
					return;

				m_LatchMode = value;
				OnLatchModeChanged.Raise(this, new BoolEventArgs(m_LatchMode));
			}
		}

		[PropertyTelemetry(DisplayLiftTelemetryNames.EXTEND_TIME, null, DisplayLiftTelemetryNames.EXTEND_TIME_CHANGED)]
		public string ExtendTime
		{
			get { return m_ExtendTime; }
			private set
			{
				if (m_ExtendTime == value)
					return;

				m_ExtendTime = value;
				OnExtendTimeChanged.Raise(this, new StringEventArgs(m_ExtendTime));
			}
		}

		[PropertyTelemetry(DisplayLiftTelemetryNames.RETRACT_TIME, null,
			DisplayLiftTelemetryNames.RETRACT_TIME_CHANGED)]
		public string RetractTime
		{
			get { return m_RetractTime; }
			private set
			{
				if (m_RetractTime == value)
					return;

				m_RetractTime = value;
				OnRetractTimeChanged.Raise(this, new StringEventArgs(m_RetractTime));
			}
		}

		/// <summary>
		/// Sets the parent telemetry provider that this instance extends.
		/// </summary>
		/// <param name="parent"></param>
		protected override void SetParent(RelayDisplayLiftDevice parent)
		{
			base.SetParent(parent);

			UpdateValues();
		}

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(RelayDisplayLiftDevice parent)
		{
			if (parent == null)
				return;

			parent.OnLatchModeChanged += ParentOnLatchModeChanged;
			parent.OnExtendTimeChanged += ParentOnExtendTimeChanged;
			parent.OnRetractTimeChanged += ParentOnRetractTimeChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(RelayDisplayLiftDevice parent)
		{
			if (parent == null)
				return;

			parent.OnLatchModeChanged -= ParentOnLatchModeChanged;
			parent.OnExtendTimeChanged -= ParentOnExtendTimeChanged;
			parent.OnRetractTimeChanged -= ParentOnRetractTimeChanged;
		}

		private void ParentOnLatchModeChanged(object sender, BoolEventArgs e)
		{
			UpdateLatchMode();
		}

		private void ParentOnExtendTimeChanged(object sender, IntEventArgs e)
		{
			UpdateExtendTime();
		}

		private void ParentOnRetractTimeChanged(object sender, IntEventArgs e)
		{
			UpdateRetractTime();
		}

		private void UpdateValues()
		{
			UpdateLatchMode();
			UpdateExtendTime();
			UpdateRetractTime();
		}

		private void UpdateLatchMode()
		{
			LatchMode = Parent != null && Parent.LatchRelay;
		}

		private void UpdateExtendTime()
		{
			ExtendTime = Parent == null ? null : Parent.ExtendTime + "ms";
		}

		private void UpdateRetractTime()
		{
			RetractTime = Parent == null ? null : Parent.RetractTime + "ms";
		}
	}
}
