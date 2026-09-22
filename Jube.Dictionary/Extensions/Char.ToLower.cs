// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    using Char = char;

    public static partial class Extensions
    {
        extension(Char c)
        {
            public Char ToLower(CultureInfo culture)
            {
                return char.ToLower(c, culture);
            }

            public Char ToLower()
            {
                return char.ToLower(c);
            }
        }
    }
}
