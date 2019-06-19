using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Displays.DisplayLift
{
    public class AbstractDisplayLiftDeviceSettings : AbstractDeviceSettings, IDisplayLiftDeviceSettings
    {
        private const string DISPLAY_ELEMENT = "Display";
        private const string BOOT_DELAY_ELEMENT = "BootDelay";
        private const string COOLING_DELAY_ELEMENT = "CoolingDelay";
        
        [PublicAPI]
        [OriginatorIdSettingsProperty(typeof(IDisplay))]
        public int? Display { get; set; }
        
        [PublicAPI]
        public int? BootDelay { get; set; }
        
        [PublicAPI]
        public int? CoolingDelay { get; set; }

        protected override void WriteElements(IcdXmlTextWriter writer)
        {
            base.WriteElements(writer);
            
            writer.WriteElementString(DISPLAY_ELEMENT, IcdXmlConvert.ToString(Display));
            writer.WriteElementString(BOOT_DELAY_ELEMENT, IcdXmlConvert.ToString(BootDelay));
            writer.WriteElementString(COOLING_DELAY_ELEMENT, IcdXmlConvert.ToString(CoolingDelay));
        }

        public override void ParseXml(string xml)
        {
            base.ParseXml(xml);

            Display = XmlUtils.TryReadChildElementContentAsInt(xml, DISPLAY_ELEMENT);
            BootDelay = XmlUtils.TryReadChildElementContentAsInt(xml, BOOT_DELAY_ELEMENT);
            CoolingDelay = XmlUtils.TryReadChildElementContentAsInt(xml, COOLING_DELAY_ELEMENT);
        }
    }
}
