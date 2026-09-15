// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static bool NotIn(this DateTime @this, params DateTime[] values)
        {
            return Array.IndexOf(values, @this) == -1;
        }
    }
}
