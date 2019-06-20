using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.DisplayLift
{
    public abstract class AbstractDisplayLiftDevice<T> : AbstractDevice<T>, IDisplayLiftDevice
        where T : IDisplayLiftDeviceSettings, new()
    {
        public event EventHandler<LiftStateChangedEventArgs> OnLiftStateChanged;
        
        private IDisplay m_Display;
        private int m_BootDelay;
        private int m_CoolingDelay;
        private eLiftState m_LiftState;
        
        private readonly IcdTimer m_BootDelayTimer;
        private readonly IcdTimer m_CooldownDelayTimer;
        
        public eLiftState LiftState
        {
            get { return m_LiftState; }
            protected set
            {
                if (m_LiftState == value)
                    return;

                m_LiftState = value;
                
                OnLiftStateChanged.Raise(this, new LiftStateChangedEventArgs(m_LiftState));

                switch (m_LiftState)
                {
                    case eLiftState.BootDelay:
                        m_BootDelayTimer.Restart(m_BootDelayTimer.Remaining);
                        break;
                    
                    case eLiftState.CooldownDelay:
                        m_CooldownDelayTimer.Restart(m_CooldownDelayTimer.Remaining);
                        break;
                }
            }
        }

        protected AbstractDisplayLiftDevice()
        {
            m_BootDelayTimer = new IcdTimer();
            m_CooldownDelayTimer = new IcdTimer();
            m_BootDelayTimer.OnElapsed += BootDelayTimerOnElapsed;
            m_CooldownDelayTimer.OnElapsed += CooldownDelayTimerOnElapsed;
            ResetTimers();
        }

        public void ExtendLift()
        {
            switch (LiftState) {
                case eLiftState.Extended:
                case eLiftState.Extending:
                case eLiftState.BootDelay:
                    return;

                case eLiftState.Retracted:
                case eLiftState.Retracting:
                case eLiftState.Unknown:
                    Extend();
                    return;
                
                case eLiftState.CooldownDelay:
                    m_CooldownDelayTimer.Stop();
                    m_BootDelayTimer.Restart(m_BootDelayTimer.Remaining);
                    return;
                default:
                    throw new NotSupportedException("Unknown Lift State. Cannot Extend Lift.");
            }
        }

        public void RetractLift()
        {
            switch (LiftState) {
                case eLiftState.Retracted:
                case eLiftState.Retracting:
                case eLiftState.CooldownDelay:
                    return;

                case eLiftState.Unknown:
                case eLiftState.Extended:
                    PowerOffDisplay();
                    LiftState = eLiftState.CooldownDelay;
                    break;
                    
                case eLiftState.Extending:
                case eLiftState.BootDelay:
                    Retract();
                    return;

                default:
                    throw new NotSupportedException("Unknown Lift State. Cannot Retract Lift.");
            }
        }
        
        private void BootDelayTimerOnElapsed(object sender, EventArgs e)
        {
            ResetTimers();
            PowerOnDisplay();
            LiftState = eLiftState.Extended;
        }
        
        private void CooldownDelayTimerOnElapsed(object sender, EventArgs e)
        {
            Retract();
        }

        private void ResetTimers()
        {
            m_BootDelayTimer.Restart(m_BootDelay);
            m_BootDelayTimer.Stop();
            m_CooldownDelayTimer.Restart(m_CoolingDelay);
            m_CooldownDelayTimer.Stop();
        }
        
        private void PowerOnDisplay()
        {
            //TODO: Actually power on the display
        }

        private void PowerOffDisplay()
        {
            //TODO: Actually power off the display
        }

        protected abstract void Extend();

        protected abstract void Retract();

        #region Settings

        protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
        {
            base.ApplySettingsFinal(settings, factory);

            IDisplay display = null;
            if (settings.Display != null)
            {
                try
                {
                    display = factory.GetDeviceById((int)settings.Display) as IDisplay;
                }
                catch (KeyNotFoundException)
                {
                    Log(eSeverity.Error, "No Display with id {0}", settings.Display);
                }
            }
            m_Display = display;

            m_BootDelay = settings.BootDelay ?? 0;
            m_CoolingDelay = settings.CoolingDelay ?? 0;
        }

        protected override void ClearSettingsFinal()
        {
            base.ClearSettingsFinal();

            m_Display = null;
        }

        protected override void CopySettingsFinal(T settings)
        {
            base.CopySettingsFinal(settings);

            settings.Display = m_Display == null ? (int?)null : m_Display.Id;
            settings.BootDelay = m_BootDelay == 0 ? (int?)null : m_BootDelay;
            settings.CoolingDelay = m_CoolingDelay == 0 ? (int?)null : m_CoolingDelay;
        }

        #endregion
    }

    public enum eLiftState
    {
        Unknown,
        Extended,
        Extending,
        BootDelay,
        Retracted,
        Retracting,
        CooldownDelay
    }

    public sealed class LiftStateChangedEventArgs : GenericEventArgs<eLiftState>
    {
        public LiftStateChangedEventArgs(eLiftState data) : base(data) { }
    }
}
