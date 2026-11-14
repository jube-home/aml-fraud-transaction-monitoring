// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Checks if the string is empty.")]
        public static bool IsEmpty(this string @this)
        {
            return @this == "";
        }
    }
}
