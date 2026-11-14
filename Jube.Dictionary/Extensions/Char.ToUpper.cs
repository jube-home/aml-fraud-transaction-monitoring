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
            [Description("Converts the character to uppercase using specified culture rules.")]
            public Char ToUpper([Description("An object that supplies culture-specific casing rules.")] CultureInfo culture)
            {
                return char.ToUpper(c, culture);
            }

            [Description("Converts the character to uppercase.")]
            public Char ToUpper()
            {
                return char.ToUpper(c);
            }
        }
    }
}
