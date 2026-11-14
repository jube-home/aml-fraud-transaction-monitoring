// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        [Description("Checks if the string is not equal to any in the provided array.")]
        public static bool NotIn(this String @this,
            [Description("The value list to compare with the object.")] params String[] values)
        {
            return Array.IndexOf(values, @this) == -1;
        }
    }
}
