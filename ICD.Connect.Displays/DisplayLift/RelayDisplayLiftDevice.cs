using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Core;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Displays.DisplayLift
{
    [ExternalTelemetry("Relay Display Lift Device Telemetry", typeof(RelayDisplayLiftExternalTelemetryProvider))]
    public sealed class RelayDisplayLiftDevice : AbstractDisplayLiftDevice<RelayDisplayLiftDeviceSettings>
    {
        public event EventHandler<BoolEventArgs> OnLatchModeChanged;
        public event EventHandler<IntEventArgs>  OnExtendTimeChanged;
        public event EventHandler<IntEventArgs>  OnRetractTimeChanged;

        private readonly IcdTimer m_ExtendTimer;
        private readonly IcdTimer m_RetractTimer;

        private bool       m_LatchRelay;
        private int        m_ExtendTime;
        private int        m_RetractTime;
        private IRelayPort m_ExtendRelay;
        private IRelayPort m_RetractRelay;

        public RelayDisplayLiftDevice()
        {
            m_ExtendTimer = new IcdTimer();
            m_ExtendTimer.OnElapsed += ExtendTimerOnElapsed;
            m_RetractTimer = new IcdTimer();
            m_RetractTimer.OnElapsed += RetractTimerOnElapsed;
        }

        public IRelayPort ExtendRelay
        {
            get { return m_ExtendRelay; }
        }

        public IRelayPort RetractRelay
        {
            get { return m_RetractRelay; }
        }

        public int ExtendTime
        {
            get { return m_ExtendTime; }
            set
            {
                if (m_ExtendTime == value)
                    return;

                m_ExtendTime = value;
                OnExtendTimeChanged.Raise(this, new IntEventArgs(m_ExtendTime));
            }
        }

        public int RetractTime
        {
            get { return m_RetractTime; }
            set
            {
                if (m_RetractTime == value)
                    return;

                m_RetractTime = value;
                OnRetractTimeChanged.Raise(this, new IntEventArgs(m_RetractTime));
            }
        }

        public bool LatchRelay
        {
            get { return m_LatchRelay; }
            set
            {
                if (m_LatchRelay == value)
                    return;

                m_LatchRelay = value;
                OnLatchModeChanged.Raise(this, new BoolEventArgs(m_LatchRelay));
            }
        }

        protected override bool GetIsOnlineStatus()
        {
            return true;
        }

        protected override void Extend()
        {
            m_RetractTimer.Stop();
            
            LiftState = eLiftState.Extending;
            if (m_LatchRelay)
            {
                ExtendLatched();
            }
            else
            {
                ExtendUnlatched();
            }
            
            m_ExtendTimer.Restart(m_ExtendTime);
        }

        protected override void Retract()
        {
            m_ExtendTimer.Stop();
            
            LiftState = eLiftState.Retracting;
            if (m_LatchRelay)
            {
                RetractLatched();
            }
            else
            {
                RetractUnlatched();
            }
            
            m_RetractTimer.Restart(m_RetractTime);
        }

        private void ExtendLatched()
        {
            if (m_ExtendRelay == null && m_RetractRelay == null)
            {
                Log(eSeverity.Error, "Cannot Extend Display Lift, relays are null.");
                return;
            }

            if (m_RetractRelay != null)
                m_RetractRelay.Open();

            if (m_ExtendRelay != null)
                m_ExtendRelay.Close();
        }

        private void ExtendUnlatched()
        {
            if (m_ExtendRelay == null || m_RetractRelay == null)
            {
                Log(eSeverity.Error, "Cannot Extend Display Lift, relays are null.");
                return;
            }

            if (m_RetractRelay.Closed)
                m_RetractRelay.Open();

            m_ExtendRelay.Close();
        }

        private void RetractLatched()
        {
            if (m_ExtendRelay == null && m_RetractRelay == null)
            {
                Log(eSeverity.Error, "Cannot Extend Display Lift, relays are null.");
                return;
            }

            if (m_ExtendRelay != null)
                m_ExtendRelay.Open();

            if (m_RetractRelay != null)
                m_RetractRelay.Close();
        }

        private void RetractUnlatched()
        {
            if (m_ExtendRelay == null || m_RetractRelay == null)
            {
                Log(eSeverity.Error, "Cannot Extend Display Lift, relays are null.");
                return;
            }

            if (m_ExtendRelay.Closed)
                m_ExtendRelay.Open();

            m_RetractRelay.Close();
        }

        #region Timer Callbacks

        private void ExtendTimerOnElapsed(object sender, EventArgs e)
        {
            if (m_ExtendRelay == null)
                return;

            if (!m_LatchRelay)
                m_ExtendRelay.Open();

            LiftState = eLiftState.BootDelay;
        }

        private void RetractTimerOnElapsed(object sender, EventArgs e)
        {
            if (m_RetractRelay == null)
                return;

            if (!m_LatchRelay)
                m_RetractRelay.Open();

            LiftState = eLiftState.Retracted;
        }

        #endregion

        #region Settings

        protected override void ApplySettingsFinal(RelayDisplayLiftDeviceSettings settings, IDeviceFactory factory)
        {
            base.ApplySettingsFinal(settings, factory);

            m_LatchRelay = settings.LatchRelay;

            IRelayPort extendRelay = null;
            if (settings.DisplayExtendRelay != null)
            {
                try
                {
                    extendRelay = factory.GetPortById((int)settings.DisplayExtendRelay) as IRelayPort;
                }
                catch (KeyNotFoundException)
                {
                    Log(eSeverity.Error, "No Relay Port with id {0}", settings.DisplayExtendRelay);
                }
            }

            m_ExtendRelay = extendRelay;

            IRelayPort retractRelay = null;
            if (settings.DisplayRetractRelay != null)
            {
                try
                {
                    retractRelay = factory.GetPortById((int)settings.DisplayRetractRelay) as IRelayPort;
                }
                catch (KeyNotFoundException)
                {
                    Log(eSeverity.Error, "No Relay Port with id {0}", settings.DisplayRetractRelay);
                }
            }

            m_RetractRelay = retractRelay;

            if (!m_LatchRelay && (m_ExtendRelay == null || m_RetractRelay == null))
            {
                Log(eSeverity.Error, "When latching mode is not enabled, both relay ports must be defined.");
            }

            m_ExtendTime = settings.ExtendTime;
            m_RetractTime = settings.RetractTime;
        }

        protected override void CopySettingsFinal(RelayDisplayLiftDeviceSettings settings)
        {
            base.CopySettingsFinal(settings);

            settings.LatchRelay = m_LatchRelay;
        }

        #endregion

        #region Console

        public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
        {
            base.BuildConsoleStatus(addRow);
            RelayDisplayLiftConsole.BuildConsoleStatus(this, addRow);
        }

        public override IEnumerable<IConsoleCommand> GetConsoleCommands()
        {
            foreach (IConsoleCommand cmd in base.GetConsoleCommands())
                yield return cmd;

            foreach (IConsoleCommand cmd in RelayDisplayLiftConsole.GetConsoleCommands(this))
                yield return cmd;
        }

        #endregion
    }
}
