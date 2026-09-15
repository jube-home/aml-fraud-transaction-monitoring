// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        public static bool NotIn(this Int32 @this, params Int32[] values)
        {
            return Array.IndexOf(values, @this) == -1;
        }
    }
}
