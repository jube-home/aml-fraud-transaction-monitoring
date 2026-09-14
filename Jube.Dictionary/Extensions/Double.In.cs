// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;

    public static partial class Extensions
    {
        public static bool In(this Double @this, params Double[] values)
        {
            return Array.IndexOf(values, @this) != -1;
        }
    }
}
