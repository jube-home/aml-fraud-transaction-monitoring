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
            [Description("JavaScript encodes a string.")]
            public String JavaScriptStringEncode()
            {
                return HttpUtility.JavaScriptStringEncode(value);
            }

            [Description("JavaScript encodes with optional double quotes.")]
            public String JavaScriptStringEncode([Description("A value that indicates whether double quotation marks will be included around the encoded string.")] Boolean addDoubleQuotes)
            {
                return HttpUtility.JavaScriptStringEncode(value, addDoubleQuotes);
            }
        }
    }
}
