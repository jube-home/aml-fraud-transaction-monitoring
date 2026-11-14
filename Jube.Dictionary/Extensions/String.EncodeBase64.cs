// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Encodes the UTF-8 bytes of a string as Base64 (ASCII text encodes exactly as before).")]
        public static string EncodeBase64(this string @this)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(@this));
        }
    }
}