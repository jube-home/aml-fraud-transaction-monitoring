// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text;
using System.Web;

namespace Jube.Dictionary.Extensions
{
    using HttpUtility = HttpUtility;
    using String = string;

    public static partial class Extensions
    {
        public static String UrlEncode(this String str)
        {
            return HttpUtility.UrlEncode(str);
        }

        public static String UrlEncode(this String str, Encoding e)
        {
            return HttpUtility.UrlEncode(str, e);
        }
    }
}
