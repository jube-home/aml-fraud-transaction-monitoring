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
            [Description("Converts the character to lowercase using specified culture rules.")]
            public Char ToLower([Description("An object that supplies culture-specific casing rules.")] CultureInfo culture)
            {
                return char.ToLower(c, culture);
            }

            [Description("Converts the character to lowercase.")]
            public Char ToLower()
            {
                return char.ToLower(c);
            }
        }
    }
}
