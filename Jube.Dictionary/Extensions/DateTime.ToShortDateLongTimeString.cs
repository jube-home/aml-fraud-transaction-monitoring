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
            public string ToShortDateLongTimeString()
            {
                return @this.ToString("G", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToShortDateLongTimeString(string culture)
            {
                return @this.ToString("G", new CultureInfo(culture));
            }

            public string ToShortDateLongTimeString(CultureInfo culture)
            {
                return @this.ToString("G", culture);
            }
        }
    }
}
