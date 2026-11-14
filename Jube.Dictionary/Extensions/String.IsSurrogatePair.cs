// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        [Description("Checks if two characters form a surrogate pair.")]
        public static Boolean IsSurrogatePair(this String s,
            [Description("The starting position of the pair of characters to evaluate.")] Int32 index)
        {
            return char.IsSurrogatePair(s, index);
        }
    }
}
