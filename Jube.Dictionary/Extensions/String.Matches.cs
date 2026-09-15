// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    using Regex = Regex;
    using String = string;

    public static partial class Extensions
    {
        public static MatchCollection Matches(this String input, String pattern)
        {
            return Regex.Matches(input, pattern);
        }

        public static MatchCollection Matches(this String input, String pattern, RegexOptions options)
        {
            return Regex.Matches(input, pattern, options);
        }
    }
}
