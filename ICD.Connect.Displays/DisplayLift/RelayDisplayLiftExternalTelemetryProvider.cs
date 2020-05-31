using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Telemetry.Providers;

namespace ICD.Connect.Displays.DisplayLift
{
	public sealed class RelayDisplayLiftExternalTelemetryProvider : IRelayDisplayLiftExternalTelemetryProvider
	{
		public event EventHandler<BoolEventArgs> OnLatchModeChanged;
		public event EventHandler<StringEventArgs> OnExtendTimeChanged;
		public event EventHandler<StringEventArgs> OnRetractTimeChanged;

		private RelayDisplayLiftDevice m_Parent;

		private bool m_LatchMode;
		private string m_ExtendTime;
		private string m_RetractTime;

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

		public void SetParent(ITelemetryProvider provider)
		{
			Unsubscribe(m_Parent);
			m_Parent = provider as RelayDisplayLiftDevice;
			Subscribe(m_Parent);

			UpdateValues();
		}

		private void Subscribe(RelayDisplayLiftDevice parent)
		{
			if (parent == null)
				return;

			parent.OnLatchModeChanged += ParentOnLatchModeChanged;
			parent.OnExtendTimeChanged += ParentOnExtendTimeChanged;
			parent.OnRetractTimeChanged += ParentOnRetractTimeChanged;
		}

		private void Unsubscribe(RelayDisplayLiftDevice parent)
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
			LatchMode = m_Parent.LatchRelay;
		}

		private void UpdateExtendTime()
		{
			ExtendTime = m_Parent.ExtendTime + "ms";
		}

		private void UpdateRetractTime()
		{
			RetractTime = m_Parent.RetractTime + "ms";
		}
	}
}
