// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;

    public static partial class Extensions
    {
        public static bool InRange(this Double @this, Double minValue, Double maxValue)
        {
            return @this.CompareTo(minValue) >= 0 && @this.CompareTo(maxValue) <= 0;
        }
    }
}
