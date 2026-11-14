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
            [Description("Returns the date formatted as RFC 1123, for example Tue, 10 Nov 2026 09:00:00 GMT.")]
            public string ToRfc1123String()
            {
                return @this.ToString("r", DateTimeFormatInfo.CurrentInfo);
            }

            [Description("Returns the date formatted as RFC 1123 using the culture.")]
            public string ToRfc1123String([Description("The culture whose month and day names are used.")] string culture)
            {
                return @this.ToString("r", new CultureInfo(culture));
            }

            [Description("Returns the date formatted as RFC 1123 using the culture.")]
            public string ToRfc1123String([Description("The culture whose month and day names are used.")] CultureInfo culture)
            {
                return @this.ToString("r", culture);
            }
        }
    }
}