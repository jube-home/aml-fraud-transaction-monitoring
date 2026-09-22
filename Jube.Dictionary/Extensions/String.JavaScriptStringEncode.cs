// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Web;

namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using HttpUtility = HttpUtility;
    using String = string;

    public static partial class Extensions
    {
        extension(String value)
        {
            public String JavaScriptStringEncode()
            {
                return HttpUtility.JavaScriptStringEncode(value);
            }

            public String JavaScriptStringEncode(Boolean addDoubleQuotes)
            {
                return HttpUtility.JavaScriptStringEncode(value, addDoubleQuotes);
            }
        }
    }
}
