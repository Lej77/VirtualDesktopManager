using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualDesktopManager.Permissions
{
    /// <summary>
    /// Help functions for XML documents.
    /// </summary>
    [Serializable]
    public static class XMLStringHelper
    {
        /// <summary>
        /// Get the XML representation of a string.
        /// </summary>
        /// <param name="text">The text to convert to XML.</param>
        /// <returns>The XML representation of the text.</returns>
        public static string FromString(string text)
        {
            string xmlString = Serialize(text);
            string startText = "<string>";
            xmlString = xmlString.Remove(0, xmlString.IndexOf(startText) + startText.Length);
            xmlString = xmlString.Remove(xmlString.LastIndexOf(startText.Insert(1, "/")));
            return xmlString;
        }

        private static string Serialize(object dataToSerialize)
        {
            if (dataToSerialize == null) return null;

            using (System.IO.StringWriter stringwriter = new System.IO.StringWriter())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(dataToSerialize.GetType());
                serializer.Serialize(stringwriter, dataToSerialize);
                return stringwriter.ToString();
            }
        }
    }
}
