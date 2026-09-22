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
            public string ToYearMonthString()
            {
                return @this.ToString("y", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToYearMonthString(string culture)
            {
                return @this.ToString("y", new CultureInfo(culture));
            }

            public string ToYearMonthString(CultureInfo culture)
            {
                return @this.ToString("y", culture);
            }
        }
    }
}
