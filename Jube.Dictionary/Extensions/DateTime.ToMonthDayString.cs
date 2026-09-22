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
            public string ToMonthDayString()
            {
                return @this.ToString("m", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToMonthDayString(string culture)
            {
                return @this.ToString("m", new CultureInfo(culture));
            }

            public string ToMonthDayString(CultureInfo culture)
            {
                return @this.ToString("m", culture);
            }
        }
    }
}
