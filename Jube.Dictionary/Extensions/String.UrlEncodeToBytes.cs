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
            [Description("URL encodes to a byte array.")]
            public byte[] UrlEncodeToBytes()
            {
                return HttpUtility.UrlEncodeToBytes(str);
            }

            [Description("URL encodes to a byte array with encoding.")]
            public byte[] UrlEncodeToBytes([Description("The encoding that specifies the encoding scheme.")] Encoding e)
            {
                return HttpUtility.UrlEncodeToBytes(str, e);
            }
        }
    }
}
