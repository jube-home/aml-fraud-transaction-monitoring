// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    using Char = char;

    public static partial class Extensions
    {
        public static Char ToUpper(this Char c, CultureInfo culture)
        {
            return char.ToUpper(c, culture);
        }

        public static Char ToUpper(this Char c)
        {
            return char.ToUpper(c);
        }
    }
}
