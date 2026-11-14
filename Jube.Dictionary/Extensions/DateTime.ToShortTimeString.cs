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
            [Description("Converts the date to a short time string.")]
            public string ToShortTimeString()
            {
                return @this.ToString("t", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a short time string using a culture.")]
            public string ToShortTimeString([Description("The culture.")] string culture)
            {
                return @this.ToString("t", new CultureInfo(culture));
            }

            [Description("Converts the date to a short time string using a culture.")]
            public string ToShortTimeString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("t", culture);
            }
        }
    }
}
