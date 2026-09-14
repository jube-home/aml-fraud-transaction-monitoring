// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static bool IsAnagram(this string @this, string otherString)
        {
            return @this
                .OrderBy(c => c)
                .SequenceEqual(otherString.OrderBy(c => c));
        }
    }
}
