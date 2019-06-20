using System;
using System.Collections.Generic;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.DisplayLift
{
    public sealed class RelayDisplayLiftDevice : AbstractDisplayLiftDevice<RelayDisplayLiftDeviceSettings>
    {
        private bool m_LatchRelay;
        private IRelayPort m_ExtendRelay;
        private IRelayPort m_RetractRelay;
        private int m_ExtendTime;
        private int m_RetractTime;
        private readonly IcdTimer m_ExtendTimer;
        private readonly IcdTimer m_RetractTimer;

        public RelayDisplayLiftDevice()
        {
            m_ExtendTimer = new IcdTimer();
            m_ExtendTimer.OnElapsed += ExtendTimerOnElapsed;
            m_RetractTimer = new IcdTimer();
            m_RetractTimer.OnElapsed += RetractTimerOnElapsed;
        }

        protected override bool GetIsOnlineStatus()
        {
            throw new System.NotImplementedException();
        }
        
        protected override void Extend()
        {
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
            
            if(m_RetractRelay.Closed)
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
            
            if(m_ExtendRelay.Closed)
                m_ExtendRelay.Open();
            
            m_RetractRelay.Close();
        }
        
        #region Timer Callbacks
        
        private void ExtendTimerOnElapsed(object sender, EventArgs e)
        {
            if (m_ExtendRelay == null)
                return;
            
            if(!m_LatchRelay)
                m_ExtendRelay.Open();

            LiftState = eLiftState.BootDelay;
        }
        private void RetractTimerOnElapsed(object sender, EventArgs e)
        {
            if (m_RetractRelay == null)
                return;
            
            if(!m_LatchRelay)
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
    }
}
