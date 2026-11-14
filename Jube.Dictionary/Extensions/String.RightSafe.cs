// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Safely gets the right part (handles out-of-range).")]
        public static string RightSafe(this string @this,
            [Description("The length of the right part to get.")] int length)
        {
            return @this.Substring(Math.Max(0, @this.Length - length));
        }
    }
}
