// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Xml;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Converts the string to an XmlDocument.")]
        public static XmlDocument ToXmlDocument(this string @this)
        {
            var doc = new XmlDocument();
            doc.LoadXml(@this);

            return doc;
        }
    }
}