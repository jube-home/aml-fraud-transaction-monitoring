// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;

    public static partial class Extensions
    {
        [Description("Checks if the value is between two values (inclusive).")]
        public static bool InRange(this Double @this,
            [Description("The minimum value.")] Double minValue,
            [Description("The maximum value.")] Double maxValue)
        {
            return @this.CompareTo(minValue) >= 0 && @this.CompareTo(maxValue) <= 0;
        }
    }
}
