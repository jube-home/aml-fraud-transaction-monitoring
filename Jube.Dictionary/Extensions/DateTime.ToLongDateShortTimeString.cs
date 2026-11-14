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
            [Description("Converts the date to a long date and short time string.")]
            public string ToLongDateShortTimeString()
            {
                return @this.ToString("f", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a long date and short time string using a culture.")]
            public string ToLongDateShortTimeString([Description("The culture.")] string culture)
            {
                return @this.ToString("f", new CultureInfo(culture));
            }

            [Description("Converts the date to a long date and short time string using a culture.")]
            public string ToLongDateShortTimeString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("f", culture);
            }
        }
    }
}
