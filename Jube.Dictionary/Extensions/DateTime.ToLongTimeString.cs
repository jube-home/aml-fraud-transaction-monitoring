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
            public string ToLongTimeString()
            {
                return @this.ToString("T", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToLongTimeString(string culture)
            {
                return @this.ToString("T", new CultureInfo(culture));
            }

            public string ToLongTimeString(CultureInfo culture)
            {
                return @this.ToString("T", culture);
            }
        }
    }
}
