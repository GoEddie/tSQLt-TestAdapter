using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace tSQLt.TestAdapter
{
    public static class RunSettingsHelper
    {
        /// <summary>
        /// Gets a string value from the tSQLt section of runsettings
        /// </summary>
        /// <param name="runSettings">The run settings from discovery context</param>
        /// <param name="elementName">The name of the element under RunSettings/tSQLt (e.g., "TestFolder")</param>
        /// <returns>The element value, or null if not found</returns>
        public static string GetTSQLtSetting(IRunSettings runSettings, string elementName)
        {
            var xml = runSettings?.SettingsXml;
            if (string.IsNullOrEmpty(xml))
            {
                return null;
            }

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                var node = xmlDoc.SelectSingleNode($"RunSettings/tSQLt/{elementName}");
                return node?.InnerText;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a boolean value from the tSQLt section of runsettings
        /// </summary>
        /// <param name="runSettings">The run settings from discovery context</param>
        /// <param name="elementName">The name of the element under RunSettings/tSQLt (e.g., "LaunchDebugger")</param>
        /// <param name="defaultValue">The default value if not found or cannot parse</param>
        /// <returns>The parsed boolean value, or defaultValue if not found/invalid</returns>
        public static bool GetTSQLtSettingBool(IRunSettings runSettings, string elementName, bool defaultValue = false)
        {
            var value = GetTSQLtSetting(runSettings, elementName);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets all values for an element from the tSQLt section of runsettings (supports multiple elements)
        /// </summary>
        /// <param name="runSettings">The run settings from discovery context</param>
        /// <param name="elementName">The name of the element under RunSettings/tSQLt (e.g., "TestFolder")</param>
        /// <returns>List of element values, or empty list if none found</returns>
        public static List<string> GetTSQLtSettings(IRunSettings runSettings, string elementName)
        {
            var results = new List<string>();
            var xml = runSettings?.SettingsXml;
            if (string.IsNullOrEmpty(xml))
            {
                return results;
            }

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                var nodes = xmlDoc.SelectNodes($"RunSettings/tSQLt/{elementName}");

                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        if (!string.IsNullOrWhiteSpace(node.InnerText))
                        {
                            results.Add(node.InnerText);
                        }
                    }
                }
            }
            catch (XmlException)
            {
                // Return empty list on error
            }

            return results;
        }
    }
}
