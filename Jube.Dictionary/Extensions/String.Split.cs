// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string[] Split(this string @this, string separator,
            StringSplitOptions option = StringSplitOptions.None)
        {
            return @this.Split(new[]
            {
                separator
            }, option);
        }
    }
}
