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
            [Description("Converts the date to a long date time string.")]
            public string ToLongDateTimeString()
            {
                return @this.ToString("F", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a long date time string using a culture.")]
            public string ToLongDateTimeString([Description("The culture.")] string culture)
            {
                return @this.ToString("F", new CultureInfo(culture));
            }

            [Description("Converts the date to a long date time string using a culture.")]
            public string ToLongDateTimeString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("F", culture);
            }
        }
    }
}
