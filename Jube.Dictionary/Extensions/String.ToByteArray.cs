// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Converts the string to a byte array.")]
        public static byte[] ToByteArray(this string @this)
        {
            var encoding = Encoding.ASCII;

            return encoding.GetBytes(@this);
        }
    }
}