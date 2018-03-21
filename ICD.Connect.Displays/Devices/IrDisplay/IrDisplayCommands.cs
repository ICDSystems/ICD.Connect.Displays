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

		private const string COMMAND_WIDE = "wide";
		private const string COMMAND_SQUARE = "square";
		private const string COMMAND_NO_SCALE = "noScale";
		private const string COMMAND_ZOOM = "zoom";

		#endregion

		#region Backing Fields

		private string m_CommandPowerOn;
		private string m_CommandPowerOff;

		private string m_CommandHdmi1;
		private string m_CommandHdmi2;
		private string m_CommandHdmi3;

		private string m_CommandWide;
		private string m_CommandSquare;
		private string m_CommandNoScale;
		private string m_CommandZoom;

		#endregion

		#region Properties

		public string CommandPowerOn { get { return m_CommandPowerOn ?? COMMAND_POWER_ON; } set { m_CommandPowerOn = value; } }
		public string CommandPowerOff { get { return m_CommandPowerOff ?? COMMAND_POWER_OFF; } set { m_CommandPowerOff = value; } }

		public string CommandHdmi1 { get { return m_CommandHdmi1 ?? COMMAND_HDMI_1; } set { m_CommandHdmi1 = value; } }
		public string CommandHdmi2 { get { return m_CommandHdmi2 ?? COMMAND_HDMI_2; } set { m_CommandHdmi2 = value; } }
		public string CommandHdmi3 { get { return m_CommandHdmi3 ?? COMMAND_HDMI_3; } set { m_CommandHdmi3 = value; } }

		public string CommandWide { get { return m_CommandWide ?? COMMAND_WIDE; } set { m_CommandWide = value; } }
		public string CommandSquare { get { return m_CommandSquare ?? COMMAND_SQUARE; } set { m_CommandSquare = value; } }
		public string CommandNoScale { get { return m_CommandNoScale ?? COMMAND_NO_SCALE; } set { m_CommandNoScale = value; } }
		public string CommandZoom { get { return m_CommandZoom ?? COMMAND_ZOOM; } set { m_CommandZoom = value; } }

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

			CommandWide = other.CommandWide;
			CommandSquare = other.CommandSquare;
			CommandNoScale = other.CommandNoScale;
			CommandZoom = other.CommandZoom;
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

			CommandWide = null;
			CommandSquare = null;
			CommandNoScale = null;
			CommandZoom = null;
		}

		#endregion
	}
}
