// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string DecodeBase64(this string @this)
        {
            return Encoding.ASCII.GetString(Convert.FromBase64String(@this));
        }
    }
}
