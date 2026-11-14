// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Replaces the string if it equals a value.")]
        public static string ReplaceWhenEquals(this string @this,
            [Description("The value to compare with.")] string oldValue,
            [Description("The replacement value.")] string newValue)
        {
            return @this == oldValue ? newValue : @this;
        }
    }
}
