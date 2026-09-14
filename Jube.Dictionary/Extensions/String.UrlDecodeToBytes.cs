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
        public static byte[] UrlDecodeToBytes(this String str)
        {
            return HttpUtility.UrlDecodeToBytes(str);
        }

        public static byte[] UrlDecodeToBytes(this String str, Encoding e)
        {
            return HttpUtility.UrlDecodeToBytes(str, e);
        }
    }
}
