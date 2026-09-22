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
            public string ToUniversalSortableLongDateTimeString()
            {
                return @this.ToString("U", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToUniversalSortableLongDateTimeString(string culture)
            {
                return @this.ToString("U", new CultureInfo(culture));
            }

            public string ToUniversalSortableLongDateTimeString(CultureInfo culture)
            {
                return @this.ToString("U", culture);
            }
        }
    }
}
