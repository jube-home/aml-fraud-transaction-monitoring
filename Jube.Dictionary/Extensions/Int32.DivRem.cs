// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using Math = Math;

    public static partial class Extensions
    {
        public static Int32 DivRem(this Int32 a, Int32 b, out Int32 result)
        {
            return Math.DivRem(a, b, out result);
        }
    }
}
