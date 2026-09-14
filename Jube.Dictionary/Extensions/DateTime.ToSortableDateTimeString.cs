// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static string ToSortableDateTimeString(this DateTime @this)
        {
            return @this.ToString("s", DateTimeFormatInfo.CurrentInfo);
        }

        public static string ToSortableDateTimeString(this DateTime @this, string culture)
        {
            return @this.ToString("s", new CultureInfo(culture));
        }

        public static string ToSortableDateTimeString(this DateTime @this, CultureInfo culture)
        {
            return @this.ToString("s", culture);
        }
    }
}
