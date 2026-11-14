// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text;
using System.Xml.Linq;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Converts the string to an XDocument.")]
        public static XDocument ToXDocument(this string @this)
        {
            var encoding = Encoding.ASCII;
            using var ms = new MemoryStream(encoding.GetBytes(@this));

            return XDocument.Load(ms);
        }
    }
}