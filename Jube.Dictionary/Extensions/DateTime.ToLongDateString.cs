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
            public string ToLongDateString()
            {
                return @this.ToString("D", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToLongDateString(string culture)
            {
                return @this.ToString("D", new CultureInfo(culture));
            }

            public string ToLongDateString(CultureInfo culture)
            {
                return @this.ToString("D", culture);
            }
        }
    }
}
