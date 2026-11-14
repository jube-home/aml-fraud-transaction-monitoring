// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Gets the substring after the specified value.")]
        public static string GetAfter(this string @this, [Description("The value to search for.")] string value)
        {
            var index = @this.IndexOf(value, StringComparison.Ordinal);

            return index == -1 ? "" : @this.Substring(index + value.Length);
        }
    }
}
