// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string Br2Nl(this string @this)
        {
            return @this.Replace("<br />", "\r\n").Replace("<br>", "\r\n");
        }
    }
}
