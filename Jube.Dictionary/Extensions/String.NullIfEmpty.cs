// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Returns null if the string is null or empty.")]
        public static string? NullIfEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
