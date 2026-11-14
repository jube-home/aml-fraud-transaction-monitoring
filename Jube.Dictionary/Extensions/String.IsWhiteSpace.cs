// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        [Description("Checks if a character at a position is whitespace.")]
        public static Boolean IsWhiteSpace(this String s,
            [Description("The position of the character to evaluate.")] Int32 index)
        {
            return char.IsWhiteSpace(s, index);
        }
    }
}
