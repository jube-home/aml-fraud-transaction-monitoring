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
            [Description("URL decodes a string.")]
            public String UrlDecode()
            {
                return HttpUtility.UrlDecode(str);
            }

            [Description("URL decodes with specified encoding.")]
            public String UrlDecode([Description("The encoding that specifies the decoding scheme.")] Encoding e)
            {
                return HttpUtility.UrlDecode(str, e);
            }
        }
    }
}
