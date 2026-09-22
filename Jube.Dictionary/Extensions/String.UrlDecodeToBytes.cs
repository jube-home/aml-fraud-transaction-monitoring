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
        extension(String str)
        {
            public byte[] UrlDecodeToBytes()
            {
                return HttpUtility.UrlDecodeToBytes(str);
            }

            public byte[] UrlDecodeToBytes(Encoding e)
            {
                return HttpUtility.UrlDecodeToBytes(str, e);
            }
        }
    }
}
