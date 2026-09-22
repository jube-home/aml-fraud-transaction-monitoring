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
            public string ToLongDateTimeString()
            {
                return @this.ToString("F", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToLongDateTimeString(string culture)
            {
                return @this.ToString("F", new CultureInfo(culture));
            }

            public string ToLongDateTimeString(CultureInfo culture)
            {
                return @this.ToString("F", culture);
            }
        }
    }
}
