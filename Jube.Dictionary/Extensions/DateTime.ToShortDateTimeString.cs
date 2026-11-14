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
            [Description("Converts the date to a short date time string.")]
            public string ToShortDateTimeString()
            {
                return @this.ToString("g", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a short date time string using a culture.")]
            public string ToShortDateTimeString([Description("The culture.")] string culture)
            {
                return @this.ToString("g", new CultureInfo(culture));
            }

            [Description("Converts the date to a short date time string using a culture.")]
            public string ToShortDateTimeString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("g", culture);
            }
        }
    }
}
