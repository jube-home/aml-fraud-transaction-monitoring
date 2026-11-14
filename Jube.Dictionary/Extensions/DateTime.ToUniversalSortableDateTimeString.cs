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
            [Description("Converts the date to a universal sortable date time string.")]
            public string ToUniversalSortableDateTimeString()
            {
                return @this.ToString("u", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a universal sortable date time string using a culture.")]
            public string ToUniversalSortableDateTimeString([Description("The culture.")] string culture)
            {
                return @this.ToString("u", new CultureInfo(culture));
            }

            [Description("Converts the date to a universal sortable date time string using a culture.")]
            public string ToUniversalSortableDateTimeString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("u", culture);
            }
        }
    }
}
