// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the smaller of two numbers.")]
        public static Double Min(this Double val1,
            [Description("The second of two double-precision floating-point numbers to compare.")] Double val2)
        {
            return Math.Min(val1, val2);
        }
    }
}
