// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using Math = Math;

    public static partial class Extensions
    {
        public static long BigMul(this Int32 a, Int32 b)
        {
            return Math.BigMul(a, b);
        }
    }
}
