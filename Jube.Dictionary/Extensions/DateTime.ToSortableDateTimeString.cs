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
            public string ToSortableDateTimeString()
            {
                return @this.ToString("s", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToSortableDateTimeString(string culture)
            {
                return @this.ToString("s", new CultureInfo(culture));
            }

            public string ToSortableDateTimeString(CultureInfo culture)
            {
                return @this.ToString("s", culture);
            }
        }
    }
}
