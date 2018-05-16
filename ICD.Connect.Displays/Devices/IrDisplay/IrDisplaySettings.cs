using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports.IrPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.Devices.IrDisplay
{
	[KrangSettings("IrDisplay", typeof(IrDisplayDevice))]
	public sealed class IrDisplaySettings : AbstractDeviceSettings, IIrDriverSettings
	{
		private const string PORT_ELEMENT = "Port";

		#region IR Command Elements

		private const string ELEMENT_IR_COMMANDS = "IrCommands";

		private const string ELEMENT_POWER = "Power";
		private const string ELEMENT_HDMI = "Hdmi";
		private const string ELEMENT_SCALE = "Scale";

		private const string ELEMENT_POWER_ON = "PowerOn";
		private const string ELEMENT_POWER_OFF = "PowerOff";

		private const string ELEMENT_HDMI_1 = "Hdmi1";
		private const string ELEMENT_HDMI_2 = "Hdmi2";
		private const string ELEMENT_HDMI_3 = "Hdmi3";

		private const string ELEMENT_WIDE = "Wide";
		private const string ELEMENT_SQUARE = "Square";
		private const string ELEMENT_NO_SCALE = "NoScale";
		private const string ELEMENT_ZOOM = "Zoom";

		#endregion

		private readonly IrDisplayCommands m_Commands;
		private readonly IrDriverProperties m_IrDriverProperties;

		#region Properties

		[OriginatorIdSettingsProperty(typeof(IIrPort))]
		public int? Port { get; set; }

		[HiddenSettingsProperty]
		public IrDisplayCommands Commands { get { return m_Commands; } }

		public string CommandPowerOn { get { return m_Commands.CommandPowerOn; } set { m_Commands.CommandPowerOn = value; } }
		public string CommandPowerOff { get { return m_Commands.CommandPowerOff; } set { m_Commands.CommandPowerOff = value; } }

		public string CommandHdmi1 { get { return m_Commands.CommandHdmi1; } set { m_Commands.CommandHdmi1 = value; } }
		public string CommandHdmi2 { get { return m_Commands.CommandHdmi2; } set { m_Commands.CommandHdmi2 = value; } }
		public string CommandHdmi3 { get { return m_Commands.CommandHdmi3; } set { m_Commands.CommandHdmi3 = value; } }

		public string CommandWide { get { return m_Commands.CommandWide; } set { m_Commands.CommandWide = value; } }
		public string CommandSquare { get { return m_Commands.CommandSquare; } set { m_Commands.CommandSquare = value; } }
		public string CommandNoScale { get { return m_Commands.CommandNoScale; } set { m_Commands.CommandNoScale = value; } }
		public string CommandZoom { get { return m_Commands.CommandZoom; } set { m_Commands.CommandZoom = value; } }

		#endregion

		#region IR Driver

		/// <summary>
		/// Gets/sets the configurable path to the IR driver.
		/// </summary>
		public string IrDriverPath
		{
			get { return m_IrDriverProperties.IrDriverPath; }
			set { m_IrDriverProperties.IrDriverPath = value; }
		}

		/// <summary>
		/// Gets/sets the configurable pulse time for the IR driver.
		/// </summary>
		public ushort IrPulseTime
		{
			get { return m_IrDriverProperties.IrPulseTime; }
			set { m_IrDriverProperties.IrPulseTime = value; }
		}

		/// <summary>
		/// Gets/sets the configurable between time for the IR driver.
		/// </summary>
		public ushort IrBetweenTime
		{
			get { return m_IrDriverProperties.IrBetweenTime; }
			set { m_IrDriverProperties.IrBetweenTime = value; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public IrDisplaySettings()
		{
			m_Commands = new IrDisplayCommands();
			m_IrDriverProperties = new IrDriverProperties();
		}

		/// <summary>
		/// Write settings elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, Port == null ? null : IcdXmlConvert.ToString((int)Port));

			writer.WriteStartElement(ELEMENT_IR_COMMANDS);
			{
				writer.WriteStartElement(ELEMENT_POWER);
				{
					writer.WriteElementString(ELEMENT_POWER_ON, CommandPowerOn);
					writer.WriteElementString(ELEMENT_POWER_OFF, CommandPowerOff);
				}
				writer.WriteEndElement();

				writer.WriteStartElement(ELEMENT_HDMI);
				{
					writer.WriteElementString(ELEMENT_HDMI_1, CommandHdmi1);
					writer.WriteElementString(ELEMENT_HDMI_2, CommandHdmi2);
					writer.WriteElementString(ELEMENT_HDMI_3, CommandHdmi3);
				}
				writer.WriteEndElement();

				writer.WriteStartElement(ELEMENT_SCALE);
				{
					writer.WriteElementString(ELEMENT_WIDE, CommandWide);
					writer.WriteElementString(ELEMENT_SQUARE, CommandSquare);
					writer.WriteElementString(ELEMENT_NO_SCALE, CommandNoScale);
					writer.WriteElementString(ELEMENT_ZOOM, CommandZoom);
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			m_IrDriverProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);

			string irCommands;
			string power = null;
			string hdmi = null;
			string scale = null;

			XmlUtils.TryGetChildElementAsString(xml, ELEMENT_IR_COMMANDS, out irCommands);

			if (irCommands != null)
			{
				XmlUtils.TryGetChildElementAsString(irCommands, ELEMENT_POWER, out power);
				XmlUtils.TryGetChildElementAsString(irCommands, ELEMENT_HDMI, out hdmi);
				XmlUtils.TryGetChildElementAsString(irCommands, ELEMENT_SCALE, out scale);
			}

			CommandPowerOn = power == null ? null : XmlUtils.TryReadChildElementContentAsString(power, ELEMENT_POWER_ON);
			CommandPowerOff = power == null ? null : XmlUtils.TryReadChildElementContentAsString(power, ELEMENT_POWER_OFF);

			CommandHdmi1 = hdmi == null ? null : XmlUtils.TryReadChildElementContentAsString(hdmi, ELEMENT_HDMI_1);
			CommandHdmi2 = hdmi == null ? null : XmlUtils.TryReadChildElementContentAsString(hdmi, ELEMENT_HDMI_2);
			CommandHdmi3 = hdmi == null ? null : XmlUtils.TryReadChildElementContentAsString(hdmi, ELEMENT_HDMI_3);

			CommandWide = scale == null ? null : XmlUtils.TryReadChildElementContentAsString(scale, ELEMENT_WIDE);
			CommandSquare = scale == null ? null : XmlUtils.TryReadChildElementContentAsString(scale, ELEMENT_SQUARE);
			CommandNoScale = scale == null ? null : XmlUtils.TryReadChildElementContentAsString(scale, ELEMENT_NO_SCALE);
			CommandZoom = scale == null ? null : XmlUtils.TryReadChildElementContentAsString(scale, ELEMENT_ZOOM);

			m_IrDriverProperties.ParseXml(xml);
		}
	}
}
