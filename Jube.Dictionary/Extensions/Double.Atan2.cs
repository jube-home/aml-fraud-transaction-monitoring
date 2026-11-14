// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the angle whose tangent is the quotient of two numbers.")]
        public static Double Atan2(this Double y, [Description("The x coordinate of a point.")] Double x)
        {
            return Math.Atan2(y, x);
        }
    }
}
