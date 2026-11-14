// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the remainder resulting from the division of the value by the divisor as specified by IEEE 754.")]
        public static Double IeeeRemainder(this Double x, [Description("The divisor.")] Double y)
        {
            return Math.IEEERemainder(x, y);
        }
    }
}