// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using Math = Math;

    public static partial class Extensions
    {
        public static Int32 Min(this Int32 val1, Int32 val2)
        {
            return Math.Min(val1, val2);
        }
    }
}
