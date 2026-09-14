// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        public static Double Atan2(this Double y, Double x)
        {
            return Math.Atan2(y, x);
        }
    }
}
