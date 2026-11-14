// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Gets the right part of the string.")]
        public static string Right(this string @this, [Description("The length of the right part to get.")] int length)
        {
            return @this.Substring(@this.Length - length);
        }
    }
}
