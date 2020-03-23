using System;

namespace ICD.Connect.Displays.Devices.IrDisplay
{
	public sealed class IrDisplayCommands
	{
		#region Defaults

		private const string COMMAND_POWER_ON = "powerOn";
		private const string COMMAND_POWER_OFF = "powerOff";

		private const string COMMAND_HDMI_1 = "hdmi1";
		private const string COMMAND_HDMI_2 = "hdmi2";
		private const string COMMAND_HDMI_3 = "hdmi3";

		#endregion

		#region Backing Fields

		private string m_CommandPowerOn;
		private string m_CommandPowerOff;

		private string m_CommandHdmi1;
		private string m_CommandHdmi2;
		private string m_CommandHdmi3;

		#endregion

		#region Properties

		public string CommandPowerOn { get { return m_CommandPowerOn ?? COMMAND_POWER_ON; } set { m_CommandPowerOn = value; } }
		public string CommandPowerOff { get { return m_CommandPowerOff ?? COMMAND_POWER_OFF; } set { m_CommandPowerOff = value; } }

		public string CommandHdmi1 { get { return m_CommandHdmi1 ?? COMMAND_HDMI_1; } set { m_CommandHdmi1 = value; } }
		public string CommandHdmi2 { get { return m_CommandHdmi2 ?? COMMAND_HDMI_2; } set { m_CommandHdmi2 = value; } }
		public string CommandHdmi3 { get { return m_CommandHdmi3 ?? COMMAND_HDMI_3; } set { m_CommandHdmi3 = value; } }

		#endregion

		#region Methods

		/// <summary>
		/// Copies the commands from the other commands instance.
		/// </summary>
		/// <param name="other"></param>
		public void Update(IrDisplayCommands other)
		{
			if (other == null)
				throw new ArgumentNullException("other");

			CommandPowerOn = other.CommandPowerOn;
			CommandPowerOff = other.CommandPowerOff;

			CommandHdmi2 = other.CommandHdmi2;
			CommandHdmi1 = other.CommandHdmi1;
			CommandHdmi3 = other.CommandHdmi3;
		}

		/// <summary>
		/// Clears the configured commands.
		/// </summary>
		public void Clear()
		{
			CommandPowerOn = null;
			CommandPowerOff = null;

			CommandHdmi2 = null;
			CommandHdmi1 = null;
			CommandHdmi3 = null;
		}

		#endregion
	}
}
