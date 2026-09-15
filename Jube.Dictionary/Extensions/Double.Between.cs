// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;

    public static partial class Extensions
    {
        public static bool Between(this Double @this, Double minValue, Double maxValue)
        {
            return minValue.CompareTo(@this) == -1 && @this.CompareTo(maxValue) == -1;
        }
    }
}
