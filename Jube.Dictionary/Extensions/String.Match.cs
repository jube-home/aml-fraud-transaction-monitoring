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
            public Match Match(String pattern)
            {
                return Regex.Match(input, pattern, RegexOptions.None, RegexSupport.MatchTimeout);
            }

            public Match Match(String pattern, RegexOptions options)
            {
                return Regex.Match(input, pattern, options, RegexSupport.MatchTimeout);
            }
        }
    }
}