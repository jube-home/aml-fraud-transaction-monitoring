// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string LeftSafe(this string @this, int length)
        {
            return @this.Substring(0, Math.Min(length, @this.Length));
        }
    }
}
