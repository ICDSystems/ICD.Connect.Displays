using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Telemetry;

namespace ICD.Connect.Displays.DisplayLift
{
    [UsedImplicitly]
    public class DisplayLiftExternalTelemetryProvider : IDisplayLiftExternalTelemetryProvider
    {
        public event EventHandler<StringEventArgs> OnLiftStateChanged;
        public event EventHandler<StringEventArgs> OnBootDelayChanged;
        public event EventHandler<StringEventArgs> OnCoolingDelayChanged;

        private IDisplayLiftDevice m_Parent;

        private string m_LiftState;
        private string m_BootDelay;
        private string m_CoolingDelay;

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

        public void SetParent(ITelemetryProvider provider)
        {
            Unsubscribe(m_Parent);
            m_Parent = provider as IDisplayLiftDevice;
            UpdateValues(m_Parent);
            Subscribe(m_Parent);
        }

        private void Subscribe(IDisplayLiftDevice parent)
        {
            if (parent == null)
                return;

            parent.OnLiftStateChanged += ParentOnLiftStateChanged;
            parent.OnBootDelayChanged += ParentOnBootDelayChanged;
            parent.OnCoolingDelayChanged += ParentOnCoolingDelayChanged;
        }

        private void Unsubscribe(IDisplayLiftDevice parent)
        {
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

        private void UpdateValues(IDisplayLiftDevice parent)
        {
            UpdateLiftState();
            UpdateBootDelay();
            UpdateCoolingDelay();
        }

        private void UpdateLiftState()
        {
            LiftState = m_Parent.LiftState.ToString();
        }

        private void UpdateBootDelay()
        {
            BootDelay = m_Parent.BootDelay + "ms";
        }

        private void UpdateCoolingDelay()
        {
            CoolingDelay = m_Parent.CoolingDelay + "ms";
        }
    }
}
