// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string GetBefore(this string @this, string value)
        {
            var index = @this.IndexOf(value, StringComparison.Ordinal);

            return index == -1 ? "" : @this.Substring(0, index);
        }
    }
}
