using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.DisplayLift
{
	public abstract class AbstractDisplayLiftDevice<T> : AbstractDevice<T>, IDisplayLiftDevice
		where T : IDisplayLiftDeviceSettings, new()
	{
		public delegate void PowerOnCallback();

		public event EventHandler<LiftStateChangedEventArgs> OnLiftStateChanged;
		public event EventHandler<IntEventArgs> OnBootDelayChanged;
		public event EventHandler<IntEventArgs> OnCoolingDelayChanged;

		private readonly IcdTimer m_BootDelayTimer;
		private readonly IcdTimer m_CooldownDelayTimer;

		private IDisplay m_Display;

		private eLiftState m_LiftState;
		private int m_BootDelay;
		private int m_CoolingDelay;
		private Action m_PostExtend;

		[CanBeNull]
		public IDisplay Display { get { return m_Display; } }

		public eLiftState LiftState
		{
			get { return m_LiftState; }
			protected set
			{
				if (m_LiftState == value)
					return;

				m_LiftState = value;

				Log(eSeverity.Debug, "Lift State: {0}", m_LiftState);

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

		public int BootDelay
		{
			get { return m_BootDelay; }
			set
			{
				m_BootDelay = value;
				ResetTimers();
				OnBootDelayChanged.Raise(this, new IntEventArgs(m_BootDelay));
			}
		}

		public int CoolingDelay
		{
			get { return m_CoolingDelay; }
			set
			{
				m_CoolingDelay = value;
				ResetTimers();
				OnCoolingDelayChanged.Raise(this, new IntEventArgs(m_CoolingDelay));
			}
		}

		public long BootDelayRemaining { get { return m_BootDelayTimer.Remaining; } }

		public long CoolingDelayRemaining { get { return m_CooldownDelayTimer.Remaining; } }

		protected AbstractDisplayLiftDevice()
		{
			m_BootDelayTimer = new IcdTimer();
			m_CooldownDelayTimer = new IcdTimer();
			m_BootDelayTimer.OnElapsed += BootDelayTimerOnElapsed;
			m_CooldownDelayTimer.OnElapsed += CooldownDelayTimerOnElapsed;
			ResetTimers();
		}

		public void ExtendLift(Action postExtend)
		{
			// Keep the callback for when we have finished extending and boot delay has elapsed
			m_PostExtend = postExtend;

			Log(eSeverity.Debug, "Extend Lift");
			switch (LiftState)
			{
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

		public void RetractLift(Action preRetract)
		{
			// We want to power off before retracting
			if (preRetract != null)
				preRetract();

			Log(eSeverity.Debug, "Retract Lift");
			switch (LiftState)
			{
				case eLiftState.Retracted:
				case eLiftState.Retracting:
				case eLiftState.CooldownDelay:
					return;

				case eLiftState.Unknown:
				case eLiftState.Extended:
					LiftState = eLiftState.CooldownDelay;
					break;

				case eLiftState.Extending:
				case eLiftState.BootDelay:
					Retract();
					m_BootDelayTimer.Stop();
					return;

				default:
					throw new NotSupportedException("Unknown Lift State. Cannot Retract Lift.");
			}
		}

		private void BootDelayTimerOnElapsed(object sender, EventArgs e)
		{
			Log(eSeverity.Debug, "Boot Delay Elapsed");
			ResetTimers();
			LiftState = eLiftState.Extended;

			if (m_PostExtend != null)
				m_PostExtend.Invoke();
		}

		private void CooldownDelayTimerOnElapsed(object sender, EventArgs e)
		{
			Log(eSeverity.Debug, "Cooldown Delay Elapsed");
			Retract();
		}

		private void ResetTimers()
		{
			m_BootDelayTimer.Restart(m_BootDelay);
			m_BootDelayTimer.Stop();
			m_CooldownDelayTimer.Restart(m_CoolingDelay);
			m_CooldownDelayTimer.Stop();
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

			SetDisplay(display);

			BootDelay = settings.BootDelay ?? 0;
			CoolingDelay = settings.CoolingDelay ?? 0;
		}

		public void SetDisplay(IDisplay display)
		{
			if (display == m_Display)
				return;

			Unsubscribe(m_Display);
			m_Display = display;
			Subscribe(m_Display);
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetDisplay(null);
		}

		protected override void CopySettingsFinal(T settings)
		{
			base.CopySettingsFinal(settings);

			settings.Display = m_Display == null ? (int?)null : m_Display.Id;
			settings.BootDelay = BootDelay == 0 ? (int?)null : BootDelay;
			settings.CoolingDelay = CoolingDelay == 0 ? (int?)null : CoolingDelay;
		}

		private void Subscribe(IDisplay display)
		{
			if (display == null)
				return;

			DisplayPowerDeviceControl powerControl = display.Controls.GetControl<DisplayPowerDeviceControl>();
			if (powerControl == null)
				return;

			powerControl.PrePowerOn = ExtendLift;
			powerControl.PrePowerOff = RetractLift;
		}

		private void Unsubscribe(IDisplay display)
		{
			if (display == null)
				return;

			DisplayPowerDeviceControl powerControl = display.Controls.GetControl<DisplayPowerDeviceControl>();
			if (powerControl == null)
				return;

			powerControl.PrePowerOn = null;
			powerControl.PrePowerOff = null;
		}

		#endregion

		#region Console

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in DisplayLiftDeviceConsole.GetConsoleCommands(this))
				yield return command;
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in DisplayLiftDeviceConsole.GetConsoleNodes(this))
				yield return node;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			DisplayLiftDeviceConsole.BuildConsoleStatus(this, addRow);
		}

		#endregion
	}
}
