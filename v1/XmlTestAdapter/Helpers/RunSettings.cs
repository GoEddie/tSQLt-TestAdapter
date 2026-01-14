using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;


namespace tSQLtTestAdapter.Helpers
{
    class RunSettings
    {
        private readonly XDocument _document;

        public RunSettings(IRunSettings runSettings)
        {

            if (runSettings == null || runSettings.SettingsXml == null || String.IsNullOrEmpty(runSettings.SettingsXml))
            {
                return;
            }

            _document = XDocument.Parse(runSettings.SettingsXml);
        }

        public string GetSetting(string name)
        {
            if (_document == null)
                return null;

            var current = _document.Element("RunSettings");
            if (current == null)
            {
                return null;
            }

            current = current.Element("TestRunParameters");

            if (current == null)
            {
                return null;
            }

            foreach (var element in current.Elements())
            {
                if (element.HasAttributes && element.Attribute("name") != null && element.Attribute("name").Value == name)
                {
                    if (element.Attribute("value") == null)
                    {
                        return null;

                    }

                    return element.Attribute("value").Value;

                }
            }

            return null;

        }


    }
}
