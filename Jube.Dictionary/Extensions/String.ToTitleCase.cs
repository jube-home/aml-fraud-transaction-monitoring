// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string ToTitleCase(this string @this)
        {
            return new CultureInfo("en-US").TextInfo.ToTitleCase(@this);
        }

        public static string ToTitleCase(this string @this, CultureInfo cultureInfo)
        {
            return cultureInfo.TextInfo.ToTitleCase(@this);
        }
    }
}
