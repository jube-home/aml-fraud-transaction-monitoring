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
            [Description("URL decodes to a byte array.")]
            public byte[] UrlDecodeToBytes()
            {
                return HttpUtility.UrlDecodeToBytes(str);
            }

            [Description("URL decodes to a byte array with encoding.")]
            public byte[] UrlDecodeToBytes([Description("The encoding object that specifies the decoding scheme.")] Encoding e)
            {
                return HttpUtility.UrlDecodeToBytes(str, e);
            }
        }
    }
}
