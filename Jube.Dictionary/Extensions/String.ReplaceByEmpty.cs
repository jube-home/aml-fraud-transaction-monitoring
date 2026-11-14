// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Replaces specified values with an empty string.")]
        public static string ReplaceByEmpty(this string @this,
            [Description("The values to replace.")] params string[] values)
        {
            foreach (var value in values)
            {
                @this = @this.Replace(value, "");
            }

            return @this;
        }
    }
}
