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
            [Description("Searches for all regex matches; matching is limited to 250 ms.")]
            public MatchCollection Matches([Description("The regular expression pattern to match.")] String pattern)
            {
                return Regex.Matches(input, pattern, RegexOptions.None, RegexSupport.matchTimeout);
            }

            [Description("Searches for all regex matches with options; matching is limited to 250 ms.")]
            public MatchCollection Matches([Description("The regular expression pattern to match.")] String pattern,
                [Description("A bitwise combination of the enumeration values that specify options for matching.")] RegexOptions options)
            {
                return Regex.Matches(input, pattern, options, RegexSupport.matchTimeout);
            }
        }
    }
}