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
            [Description("Converts the date to a short date string.")]
            public string ToShortDateString()
            {
                return @this.ToString("d", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a short date string using a culture.")]
            public string ToShortDateString([Description("The culture.")] string culture)
            {
                return @this.ToString("d", new CultureInfo(culture));
            }

            [Description("Converts the date to a short date string using a culture.")]
            public string ToShortDateString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("d", culture);
            }
        }
    }
}
