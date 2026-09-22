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
            public string ToShortDateString()
            {
                return @this.ToString("d", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToShortDateString(string culture)
            {
                return @this.ToString("d", new CultureInfo(culture));
            }

            public string ToShortDateString(CultureInfo culture)
            {
                return @this.ToString("d", culture);
            }
        }
    }
}
