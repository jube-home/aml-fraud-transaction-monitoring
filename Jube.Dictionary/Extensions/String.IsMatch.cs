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
            public Boolean IsMatch(String pattern)
            {
                return Regex.IsMatch(input, pattern);
            }

            public Boolean IsMatch(String pattern, RegexOptions options)
            {
                return Regex.IsMatch(input, pattern, options);
            }
        }
    }
}
