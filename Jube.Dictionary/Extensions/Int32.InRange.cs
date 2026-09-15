// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        public static bool InRange(this Int32 @this, Int32 minValue, Int32 maxValue)
        {
            return @this.CompareTo(minValue) >= 0 && @this.CompareTo(maxValue) <= 0;
        }
    }
}
