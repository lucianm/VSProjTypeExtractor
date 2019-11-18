/*
    VSProjTypeExtractor - Visual Studio project type GUID extractor
    SimpleXmlCfgReader.cs - simple configuration file reader
    Copyright (c) 2019, Lucian Muresan.

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

    You can contact the author at :
    - VSProjTypeExtractor homepage and source repository : https://github.com/lucianm/VSProjTypeExtractor
*/

using System;
using System.Xml;
using System.IO;

namespace VSProjTypeExtractorManaged
{
    public sealed class SimpleXmlCfgReader
    {
        private XmlDocument _xmlDoc;
        private ConAndLog conlog = ConAndLog.Instance;
        public SimpleXmlCfgReader(string filePath)
        {
            string XmlFileText = "";
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        XmlFileText += line;
                    }
                }

                //
                _xmlDoc = new XmlDocument();
                _xmlDoc.LoadXml(XmlFileText);
            }
            catch (Exception e)
            {
                throw new Exception("The config file '" + filePath + "' could not be loaded: ", e);
            }
        }
        ~SimpleXmlCfgReader()
        {

        }

        public string GetTextValueAtNode(string strNodePath, string strDefaultVal)
        {
            string strValue = "";
            try
            {
                strValue = _xmlDoc.SelectSingleNode(strNodePath).InnerText;
            }
            catch (Exception e)
            {
                conlog.WriteLine("The value at xpath '" + strNodePath + "' could not be read:");
                conlog.WriteLine(e.Message);
            }
            if (strValue == "")
            {
                strValue = strDefaultVal;
                //conlog.WriteLine("The value at xpath '" + strNodePath + "' has been replaced with the default: " + strDefaultVal);
            }
            return strValue;
        }
    }
}
