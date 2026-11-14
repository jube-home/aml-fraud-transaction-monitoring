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
            [Description("Converts the date to a month and day string.")]
            public string ToMonthDayString()
            {
                return @this.ToString("m", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a month and day string using a culture.")]
            public string ToMonthDayString([Description("The culture.")] string culture)
            {
                return @this.ToString("m", new CultureInfo(culture));
            }

            [Description("Converts the date to a month and day string using a culture.")]
            public string ToMonthDayString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("m", culture);
            }
        }
    }
}
