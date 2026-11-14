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
            [Description("Converts the date to a long time string.")]
            public string ToLongTimeString()
            {
                return @this.ToString("T", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a long time string using a culture.")]
            public string ToLongTimeString([Description("The culture.")] string culture)
            {
                return @this.ToString("T", new CultureInfo(culture));
            }

            [Description("Converts the date to a long time string using a culture.")]
            public string ToLongTimeString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("T", culture);
            }
        }
    }
}
