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
            [Description("Converts the date to a long date string.")]
            public string ToLongDateString()
            {
                return @this.ToString("D", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a long date string using a culture.")]
            public string ToLongDateString([Description("The culture.")] string culture)
            {
                return @this.ToString("D", new CultureInfo(culture));
            }

            [Description("Converts the date to a long date string using a culture.")]
            public string ToLongDateString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("D", culture);
            }
        }
    }
}
