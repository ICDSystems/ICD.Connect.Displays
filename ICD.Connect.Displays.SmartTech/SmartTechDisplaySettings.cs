using System;
using ICD.Common.Properties;
using ICD.Connect.Displays.Settings;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Displays.SmartTech
{
    public sealed class SmartTechDisplaySettings : AbstractDisplayWithAudioSettings
    {
        private const string FACTORY_NAME = "SmartTechDisplay";
        /// <summary>
        /// Gets the originator factory name.
        /// </summary>
        public override string FactoryName { get { return FACTORY_NAME; } }

        /// <summary>
        /// Gets the type of the originator for this settings instance.
        /// </summary>
        public override Type OriginatorType { get { return typeof(SmartTechDisplay); } }

        /// <summary>
        /// Loads the settings from XML.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        [PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
        public static SmartTechDisplaySettings FromXml(string xml)
        {
            SmartTechDisplaySettings output = new SmartTechDisplaySettings();
            output.ParseXml(xml);
            return output;
        }
    }
}