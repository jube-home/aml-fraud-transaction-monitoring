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
            [Description("URL encodes a string.")]
            public String UrlEncode()
            {
                return HttpUtility.UrlEncode(str);
            }

            [Description("URL encodes with specified encoding.")]
            public String UrlEncode([Description("The encoding object that specifies the encoding scheme.")] Encoding e)
            {
                return HttpUtility.UrlEncode(str, e);
            }
        }
    }
}
