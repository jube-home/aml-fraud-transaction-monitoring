// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Splits the string using a separator.")]
        public static string[] Split(this string @this,
            [Description("A string that delimit the substrings in this string.")] string separator,
            [Description("Specify RemoveEmptyEntries to omit empty array elements, or None to include empty array elements.")] StringSplitOptions option = StringSplitOptions.None)
        {
            return @this.Split(new[]
            {
                separator
            }, option);
        }
    }
}
