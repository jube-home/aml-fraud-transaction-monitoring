// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Checks if the string matches a pattern with wildcards; matching is limited to 250 ms.")]
        public static bool IsLike(this string @this,
            [Description("The pattern to use. Use '*' as wildcard string.")] string pattern)
        {
            var regexPattern = "^" + Regex.Escape(pattern) + "$";

            regexPattern = regexPattern.Replace(@"\[!", "[^")
                .Replace(@"\[", "[")
                .Replace(@"\]", "]")
                .Replace(@"\?", ".")
                .Replace(@"\*", ".*")
                .Replace(@"\#", @"\d");

            return Regex.IsMatch(@this, regexPattern, RegexOptions.None, RegexSupport.matchTimeout);
        }
    }
}