// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        [Description("Checks if the value is between two values (inclusive).")]
        public static bool InRange(this Int32 @this,
            [Description("The minimum value.")] Int32 minValue,
            [Description("The maximum value.")] Int32 maxValue)
        {
            return @this.CompareTo(minValue) >= 0 && @this.CompareTo(maxValue) <= 0;
        }
    }
}
