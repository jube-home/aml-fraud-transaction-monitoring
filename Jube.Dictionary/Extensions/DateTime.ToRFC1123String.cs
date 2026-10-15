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
            public string ToRfc1123String()
            {
                return @this.ToString("r", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToRfc1123String(string culture)
            {
                return @this.ToString("r", new CultureInfo(culture));
            }

            public string ToRfc1123String(CultureInfo culture)
            {
                return @this.ToString("r", culture);
            }
        }
    }
}