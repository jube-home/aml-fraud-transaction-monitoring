// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string GetBetween(this string @this, string before, string after)
        {
            var beforeStartIndex = @this.IndexOf(before, StringComparison.Ordinal);
            if (beforeStartIndex == -1)
            {
                return "";
            }

            var startIndex = beforeStartIndex + before.Length;
            var afterStartIndex = @this.IndexOf(after, startIndex, StringComparison.Ordinal);

            if (afterStartIndex == -1)
            {
                return "";
            }

            return @this.Substring(startIndex, afterStartIndex - startIndex);
        }
    }
}
