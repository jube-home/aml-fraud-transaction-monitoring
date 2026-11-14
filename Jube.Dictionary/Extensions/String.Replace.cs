// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Replaces a substring at a specific position.")]
        public static string Replace(this string @this,
            [Description("The start index.")] int startIndex,
            [Description("The length.")] int length,
            [Description("The replacement value.")] string value)
        {
            @this = @this.Remove(startIndex, length).Insert(startIndex, value);

            return @this;
        }
    }
}
