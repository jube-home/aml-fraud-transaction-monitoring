// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using Regex = Regex;
    using String = string;

    public static partial class Extensions
    {
        extension(String input)
        {
            [Description("Checks if the string matches a regular expression; matching is limited to 250 ms.")]
            public Boolean IsMatch([Description("The regular expression pattern to match.")] String pattern)
            {
                return Regex.IsMatch(input, pattern, RegexOptions.None, RegexSupport.matchTimeout);
            }

            [Description("Checks if the string matches a regex with options; matching is limited to 250 ms.")]
            public Boolean IsMatch([Description("The regular expression pattern to match.")] String pattern,
                [Description("A bitwise combination of the enumeration values that provide options for matching.")] RegexOptions options)
            {
                return Regex.IsMatch(input, pattern, options, RegexSupport.matchTimeout);
            }
        }
    }
}