// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Concatenates the string with others.")]
        public static string ConcatWith(this string @this,
            [Description("The strings to concatenate with.")] params string[] values)
        {
            return string.Concat(@this, string.Concat(values));
        }
    }
}
