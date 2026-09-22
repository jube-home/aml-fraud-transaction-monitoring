// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        extension(DateTime @this)
        {
            public string ToLongDateShortTimeString()
            {
                return @this.ToString("f", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToLongDateShortTimeString(string culture)
            {
                return @this.ToString("f", new CultureInfo(culture));
            }

            public string ToLongDateShortTimeString(CultureInfo culture)
            {
                return @this.ToString("f", culture);
            }
        }
    }
}
