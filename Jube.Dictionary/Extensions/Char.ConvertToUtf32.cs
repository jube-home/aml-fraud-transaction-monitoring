// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Char = char;
    using Int32 = int;

    public static partial class Extensions
    {
        [Description("Converts a UTF-16 surrogate pair into a Unicode code point.")]
        public static Int32 ConvertToUtf32(this Char highSurrogate,
            [Description("A low surrogate code unit (U+DC00 through U+DFFF).")] Char lowSurrogate)
        {
            return char.ConvertToUtf32(highSurrogate, lowSurrogate);
        }
    }
}
