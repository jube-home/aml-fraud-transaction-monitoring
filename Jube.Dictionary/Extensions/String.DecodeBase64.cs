// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Decodes Base64 to UTF-8 text.")]
        public static string DecodeBase64(this string @this)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(@this));
        }
    }
}