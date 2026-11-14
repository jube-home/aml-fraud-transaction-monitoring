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
            [Description("Converts the date to a short date and long time string.")]
            public string ToShortDateLongTimeString()
            {
                return @this.ToString("G", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a short date and long time string using a culture.")]
            public string ToShortDateLongTimeString([Description("The culture.")] string culture)
            {
                return @this.ToString("G", new CultureInfo(culture));
            }

            [Description("Converts the date to a short date and long time string using a culture.")]
            public string ToShortDateLongTimeString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("G", culture);
            }
        }
    }
}
