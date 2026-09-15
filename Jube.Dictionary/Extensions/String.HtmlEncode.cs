// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Web;

namespace Jube.Dictionary.Extensions
{
    using HttpUtility = HttpUtility;
    using String = string;

    public static partial class Extensions
    {
        public static String HtmlEncode(this String s)
        {
            return HttpUtility.HtmlEncode(s);
        }

        public static void HtmlEncode(this String s, TextWriter output)
        {
            HttpUtility.HtmlEncode(s, output);
        }
    }
}
