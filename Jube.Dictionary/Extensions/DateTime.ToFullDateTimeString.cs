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
            public string ToFullDateTimeString()
            {
                return @this.ToString("F", DateTimeFormatInfo.CurrentInfo);
            }

            public string ToFullDateTimeString(string culture)
            {
                return @this.ToString("F", new CultureInfo(culture));
            }

            public string ToFullDateTimeString(CultureInfo culture)
            {
                return @this.ToString("F", culture);
            }
        }
    }
}
