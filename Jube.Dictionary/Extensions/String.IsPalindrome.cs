// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static bool IsPalindrome(this string @this)
        {
            var rgx = new Regex("[^a-zA-Z0-9]");
            var normalised = rgx.Replace(@this, "").ToLowerInvariant();
            return normalised.SequenceEqual(normalised.Reverse());
        }
    }
}
