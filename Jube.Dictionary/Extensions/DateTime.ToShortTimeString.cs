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
            public string ToShortTimeString()
            {
                return @this.ToString("t", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToShortTimeString(string culture)
            {
                return @this.ToString("t", new CultureInfo(culture));
            }

            public string ToShortTimeString(CultureInfo culture)
            {
                return @this.ToString("t", culture);
            }
        }
    }
}
