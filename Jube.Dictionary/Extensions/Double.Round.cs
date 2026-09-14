// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Int32 = int;
    using Math = Math;

    public static partial class Extensions
    {
        public static Double Round(this Double a)
        {
            return Math.Round(a);
        }

        public static Double Round(this Double a, Int32 digits)
        {
            return Math.Round(a, digits);
        }

        public static Double Round(this Double a, MidpointRounding mode)
        {
            return Math.Round(a, mode);
        }

        public static Double Round(this Double a, Int32 digits, MidpointRounding mode)
        {
            return Math.Round(a, digits, mode);
        }
    }
}
