// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Safely gets the left part (handles out-of-range).")]
        public static string LeftSafe(this string @this,
            [Description("The length of the left part to get.")] int length)
        {
            return @this.Substring(0, Math.Min(length, @this.Length));
        }
    }
}
