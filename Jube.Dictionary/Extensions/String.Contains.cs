// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static bool Contains(this string @this, string value)
        {
            return @this.IndexOf(value, StringComparison.Ordinal) != -1;
        }

        public static bool Contains(this string @this, string value, StringComparison comparisonType)
        {
            return @this.IndexOf(value, comparisonType) != -1;
        }
    }
}
