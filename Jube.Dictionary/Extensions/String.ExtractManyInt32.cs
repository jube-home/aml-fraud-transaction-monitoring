// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static int[] ExtractManyInt32(this string @this)
        {
            return Regex.Matches(@this, @"[-]?\d+")
                .Select(x => Convert.ToInt32(x.Value))
                .ToArray();
        }
    }
}
