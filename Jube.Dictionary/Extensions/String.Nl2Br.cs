// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string Nl2Br(this string @this)
        {
            return @this.Replace("\r\n", "<br />").Replace("\n", "<br />");
        }
    }
}
