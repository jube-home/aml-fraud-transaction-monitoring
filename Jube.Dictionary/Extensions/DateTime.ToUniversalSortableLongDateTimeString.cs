// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static string ToUniversalSortableLongDateTimeString(this DateTime @this)
        {
            return @this.ToString("U", DateTimeFormatInfo.CurrentInfo);
        }

        public static string ToUniversalSortableLongDateTimeString(this DateTime @this, string culture)
        {
            return @this.ToString("U", new CultureInfo(culture));
        }

        public static string ToUniversalSortableLongDateTimeString(this DateTime @this, CultureInfo culture)
        {
            return @this.ToString("U", culture);
        }
    }
}
