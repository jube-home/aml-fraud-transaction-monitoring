// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static bool ContainsAll(this string @this, params string[] values)
        {
            foreach (var value in values)
            {
                if (@this.IndexOf(value, StringComparison.Ordinal) == -1)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ContainsAll(this string @this, StringComparison comparisonType, params string[] values)
        {
            foreach (var value in values)
            {
                if (@this.IndexOf(value, comparisonType) == -1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
