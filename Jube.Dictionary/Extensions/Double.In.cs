// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;

    public static partial class Extensions
    {
        [Description("Checks if the value is equal to any in the provided array.")]
        public static bool In(this Double @this,
            [Description("The value list to compare with the object.")] params Double[] values)
        {
            return Array.IndexOf(values, @this) != -1;
        }
    }
}
