// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        [Description("Checks if the value is not equal to any in the provided array.")]
        public static bool NotIn(this Int32 @this,
            [Description("The value list to compare with the object.")] params Int32[] values)
        {
            return Array.IndexOf(values, @this) == -1;
        }
    }
}
