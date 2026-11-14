// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns a number raised to a specified power.")]
        public static Double Pow(this Double x,
            [Description("A double-precision floating-point number that specifies a power.")] Double y)
        {
            return Math.Pow(x, y);
        }
    }
}
