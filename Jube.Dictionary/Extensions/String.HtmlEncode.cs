// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Web;

namespace Jube.Dictionary.Extensions
{
    using HttpUtility = HttpUtility;
    using String = string;

    public static partial class Extensions
    {
        extension(String s)
        {
            public String HtmlEncode()
            {
                return HttpUtility.HtmlEncode(s);
            }

            public void HtmlEncode(TextWriter output)
            {
                HttpUtility.HtmlEncode(s, output);
            }
        }
    }
}
