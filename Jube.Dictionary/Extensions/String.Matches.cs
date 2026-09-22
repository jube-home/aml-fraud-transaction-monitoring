// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    using Regex = Regex;
    using String = string;

    public static partial class Extensions
    {
        extension(String input)
        {
            public MatchCollection Matches(String pattern)
            {
                return Regex.Matches(input, pattern);
            }

            public MatchCollection Matches(String pattern, RegexOptions options)
            {
                return Regex.Matches(input, pattern, options);
            }
        }
    }
}
