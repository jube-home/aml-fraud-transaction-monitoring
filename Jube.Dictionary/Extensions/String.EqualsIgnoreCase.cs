// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Checks if two strings are equal ignoring case.")]
        public static bool EqualsIgnoreCase(this string @this,
            [Description("The string to compare with.")] string comparedString)
        {
            return @this.Equals(comparedString, StringComparison.OrdinalIgnoreCase);
        }
    }
}
