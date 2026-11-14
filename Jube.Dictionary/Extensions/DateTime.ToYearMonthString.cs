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
            [Description("Converts the date to a year and month string.")]
            public string ToYearMonthString()
            {
                return @this.ToString("y", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a year and month string using a culture.")]
            public string ToYearMonthString([Description("The culture.")] string culture)
            {
                return @this.ToString("y", new CultureInfo(culture));
            }

            [Description("Converts the date to a year and month string using a culture.")]
            public string ToYearMonthString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("y", culture);
            }
        }
    }
}
