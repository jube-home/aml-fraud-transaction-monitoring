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
            public Char ToUpper(CultureInfo culture)
            {
                return char.ToUpper(c, culture);
            }

            public Char ToUpper()
            {
                return char.ToUpper(c);
            }
        }
    }
}
