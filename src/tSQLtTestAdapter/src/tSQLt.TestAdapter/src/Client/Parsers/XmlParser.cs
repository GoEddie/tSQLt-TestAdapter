using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace tSQLt.TestAdapter.Client.Parsers
{
    public static class XmlParser
    {
        public static TestSuites Get(string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof (TestSuites));
                return serializer.Deserialize(XmlReader.Create(new StringReader(xml))) as TestSuites;
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("tSQLt Test Runner unable to deserialize xml error: {0}, xml: \r\n{1}\r\n", ex.Message, xml);
                }

                return new TestSuites()
                {
                    Suites = new List<TestSuite>() { new TestSuite(ex.Message) }
                };
            }
        }
    }
}
