// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Returns a default value if the string is empty.")]
        public static string IfEmpty(this string value,
            [Description("The default value to return if empty.")] string defaultValue)
        {
            return value.IsNotEmpty() ? value : defaultValue;
        }
    }
}
