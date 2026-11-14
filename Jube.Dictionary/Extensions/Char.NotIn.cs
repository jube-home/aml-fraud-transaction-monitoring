// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Char = char;

    public static partial class Extensions
    {
        [Description("Checks if the character is not equal to any in the provided array.")]
        public static bool NotIn(this Char @this,
            [Description("The value list to compare with the object.")] params Char[] values)
        {
            return Array.IndexOf(values, @this) == -1;
        }
    }
}
