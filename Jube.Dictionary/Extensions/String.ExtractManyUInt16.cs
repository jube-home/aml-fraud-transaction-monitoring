// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static ushort[] ExtractManyUInt16(this string @this)
        {
            return Regex.Matches(@this, @"\d+")
                .Select(x => Convert.ToUInt16(x.Value))
                .ToArray();
        }
    }
}
