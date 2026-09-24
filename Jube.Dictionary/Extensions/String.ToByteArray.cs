// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static byte[] ToByteArray(this string @this)
        {
            var encoding = Encoding.ASCII;
            return encoding.GetBytes(@this);
        }
    }
}