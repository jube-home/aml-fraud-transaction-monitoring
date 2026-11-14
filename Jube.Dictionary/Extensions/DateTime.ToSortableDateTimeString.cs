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
            [Description("Converts the date to a sortable date time string.")]
            public string ToSortableDateTimeString()
            {
                return @this.ToString("s", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Converts the date to a sortable date time string using a culture.")]
            public string ToSortableDateTimeString([Description("The culture.")] string culture)
            {
                return @this.ToString("s", new CultureInfo(culture));
            }

            [Description("Converts the date to a sortable date time string using a culture.")]
            public string ToSortableDateTimeString([Description("The culture.")] CultureInfo culture)
            {
                return @this.ToString("s", culture);
            }
        }
    }
}
