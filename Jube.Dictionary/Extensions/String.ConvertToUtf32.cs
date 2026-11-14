// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        [Description("Converts a character or surrogate pair to a Unicode code point.")]
        public static Int32 ConvertToUtf32(this String s,
            [Description("The index position of the character or surrogate pair.")] Int32 index)
        {
            return char.ConvertToUtf32(s, index);
        }
    }
}
