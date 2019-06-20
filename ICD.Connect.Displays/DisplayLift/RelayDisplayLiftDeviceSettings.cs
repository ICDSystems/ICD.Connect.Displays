using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Protocol.Ports.RelayPort;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.DisplayLift
{
    public sealed class RelayDisplayLiftDeviceSettings : AbstractDisplayLiftDeviceSettings, IRelayDisplayLiftDeviceSettings
    {
        private const string DISPLAY_EXTEND_RELAY_ELEMENT = "DisplayExtendRelay";
        private const string DISPLAY_RETRACT_RELAY_ELEMENT = "DisplayRetractRelay";
        private const string LATCH_RELAY_ELEMENT = "LatchRelay";
        private const string EXTEND_TIME = "ExtendTime";
        private const string RETRACT_TIME = "RetractTime";
        
        [PublicAPI]
        [OriginatorIdSettingsProperty(typeof(IRelayPort))]
        public int? DisplayExtendRelay { get; set; }
        
        [PublicAPI]
        [OriginatorIdSettingsProperty(typeof(IRelayPort))]
        public int? DisplayRetractRelay { get; set; }
        
        [PublicAPI]
        public bool LatchRelay { get; set; }
        
        [PublicAPI]
        public int ExtendTime { get; set; }
        
        [PublicAPI]
        public int RetractTime { get; set; }

        protected override void WriteElements(IcdXmlTextWriter writer)
        {
            base.WriteElements(writer);
            
            writer.WriteElementString(DISPLAY_EXTEND_RELAY_ELEMENT, IcdXmlConvert.ToString(DisplayExtendRelay));
            writer.WriteElementString(DISPLAY_RETRACT_RELAY_ELEMENT, IcdXmlConvert.ToString(DisplayRetractRelay));
            writer.WriteElementString(LATCH_RELAY_ELEMENT, IcdXmlConvert.ToString(LatchRelay));
            writer.WriteElementString(EXTEND_TIME, IcdXmlConvert.ToString(ExtendTime));
            writer.WriteElementString(RETRACT_TIME, IcdXmlConvert.ToString(RetractTime));
        }

        public override void ParseXml(string xml)
        {
            base.ParseXml(xml);

            DisplayExtendRelay = XmlUtils.TryReadChildElementContentAsInt(xml, DISPLAY_EXTEND_RELAY_ELEMENT);
            DisplayRetractRelay = XmlUtils.TryReadChildElementContentAsInt(xml, DISPLAY_RETRACT_RELAY_ELEMENT);
            LatchRelay = XmlUtils.TryReadChildElementContentAsBoolean(xml, LATCH_RELAY_ELEMENT) ?? false;
            ExtendTime = XmlUtils.TryReadChildElementContentAsInt(xml, EXTEND_TIME) ?? 5000;
            RetractTime = XmlUtils.TryReadChildElementContentAsInt(xml, RETRACT_TIME) ?? 5000;
        }
    }
}
