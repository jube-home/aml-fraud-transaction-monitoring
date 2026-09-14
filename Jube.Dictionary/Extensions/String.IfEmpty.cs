// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string IfEmpty(this string value, string defaultValue)
        {
            return value.IsNotEmpty() ? value : defaultValue;
        }
    }
}
