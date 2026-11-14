// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;

    public static partial class Extensions
    {
        [Description("Checks if the value is between two values (exclusive).")]
        public static bool Between(this Double @this,
            [Description("The minimum value.")] Double minValue,
            [Description("The maximum value.")] Double maxValue)
        {
            return minValue.CompareTo(@this) == -1 && @this.CompareTo(maxValue) == -1;
        }
    }
}
