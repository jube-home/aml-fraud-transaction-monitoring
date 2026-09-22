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
            public string ToUniversalSortableDateTimeString()
            {
                return @this.ToString("u", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToUniversalSortableDateTimeString(string culture)
            {
                return @this.ToString("u", new CultureInfo(culture));
            }

            public string ToUniversalSortableDateTimeString(CultureInfo culture)
            {
                return @this.ToString("u", culture);
            }
        }
    }
}
