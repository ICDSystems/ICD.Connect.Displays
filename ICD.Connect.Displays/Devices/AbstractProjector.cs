using ICD.Connect.Displays.Settings;
using System;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Devices
{
	public abstract class AbstractProjector<T> : AbstractDisplay<T>, IProjector where T : IProjectorSettings, new()
	{

		public event EventHandler<ProjectorLampHoursApiEventArgs> OnLampHoursUpdated;

		private int m_LampHours;

		/// <summary>
		/// Expected warming duration for the projector in ms
		/// </summary>
		private long m_WarmingTime;

		/// <summary>
		/// Expected cooling duration for the projector in ms
		/// </summary>
		private long m_CoolingTime;

		public int LampHours
		{
			get { return m_LampHours; }
			protected set
			{
				if (value == m_LampHours)
					return;

				m_LampHours = value;

				OnLampHoursUpdated.Raise(this, new ProjectorLampHoursApiEventArgs(value));
			}
		}

		protected override void RaisePowerStateChanged(ePowerState state)
		{
			switch (state)
			{
				case ePowerState.Cooling:
					RaisePowerStateChanged(state, m_CoolingTime);
					break;
				case ePowerState.Warming:
					RaisePowerStateChanged(state, m_WarmingTime);
					break;
				default:
					base.RaisePowerStateChanged(state);
					break;
			}
		}

		#region Settings

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(T settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_WarmingTime = settings.WarmingTime;
			m_CoolingTime = settings.CoolingTime;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_WarmingTime = 0;
			m_CoolingTime = 0;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(T settings)
		{
			base.CopySettingsFinal(settings);

			settings.WarmingTime = m_WarmingTime;
			settings.CoolingTime = m_CoolingTime;
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Warming Time", m_WarmingTime);
			addRow("Cooling Time", m_CoolingTime);
			addRow("Lamp Hours", LampHours);
		}

		#endregion

	}
}
