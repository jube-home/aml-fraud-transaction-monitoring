// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Web;

namespace Jube.Dictionary.Extensions
{
    using HttpUtility = HttpUtility;
    using String = string;

    public static partial class Extensions
    {
        public static String HtmlAttributeEncode(this String s)
        {
            return HttpUtility.HtmlAttributeEncode(s);
        }

        public static void HtmlAttributeEncode(this String s, TextWriter output)
        {
            HttpUtility.HtmlAttributeEncode(s, output);
        }
    }
}
