// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        [Description("Gets the Unicode category of a character at a position.")]
        public static UnicodeCategory GetUnicodeCategory(this String s,
            [Description("The character position in the string.")] Int32 index)
        {
            return char.GetUnicodeCategory(s, index);
        }
    }
}
