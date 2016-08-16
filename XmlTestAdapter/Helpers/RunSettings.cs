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

            if (String.IsNullOrEmpty(runSettings.SettingsXml))
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
                throw new InvalidOperationException("You must supply a runSettings and with a connectionString");
            }

            current = current.Element("TestRunParameters");

            if (current == null)
            {
                throw new InvalidOperationException(
                    "You must supply a runSettings with a TestRunParameters section with a connectionString");
            }

            foreach (var element in current.Elements())
            {
                if (element.HasAttributes && element.Attribute("name") != null && element.Attribute("name").Value == name)
                {
                    if (element.Attribute("value") == null)
                    {
                        throw new InvalidOperationException(
                            "You must supply a runSettings with a TestRunParameters section with a connectionString - it looks like you have the element but are missing the attribute \"value\"");

                    }

                    return element.Attribute("value").Value;

                }
            }

            throw new InvalidOperationException(
                "You must supply a runSettings with a TestRunParameters section with a connectionString - nope not found :(");

        }


    }
}
