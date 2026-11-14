// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Gets the left part of the string.")]
        public static string Left(this string @this, [Description("The length of the left part to get.")] int length)
        {
            return @this.Substring(0, length);
        }
    }
}
