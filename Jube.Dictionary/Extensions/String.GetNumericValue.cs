// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        [Description("Gets the numeric value of a character at a position.")]
        public static Double GetNumericValue(this String s,
            [Description("The character position in the string.")] Int32 index)
        {
            return char.GetNumericValue(s, index);
        }
    }
}
