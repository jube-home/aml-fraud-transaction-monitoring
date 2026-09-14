// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static bool EqualsIgnoreCase(this string @this, string comparedString)
        {
            return @this.Equals(comparedString, StringComparison.OrdinalIgnoreCase);
        }
    }
}
