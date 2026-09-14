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
        public static byte[] UrlEncodeToBytes(this String str)
        {
            return HttpUtility.UrlEncodeToBytes(str);
        }

        public static byte[] UrlEncodeToBytes(this String str, Encoding e)
        {
            return HttpUtility.UrlEncodeToBytes(str, e);
        }
    }
}
