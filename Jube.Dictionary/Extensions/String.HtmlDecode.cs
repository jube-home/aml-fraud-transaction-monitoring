// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Web;

namespace Jube.Dictionary.Extensions
{
    using HttpUtility = HttpUtility;
    using String = string;

    public static partial class Extensions
    {
        public static String HtmlDecode(this String s)
        {
            return HttpUtility.HtmlDecode(s);
        }

        public static void HtmlDecode(this String s, TextWriter output)
        {
            HttpUtility.HtmlDecode(s, output);
        }
    }
}
