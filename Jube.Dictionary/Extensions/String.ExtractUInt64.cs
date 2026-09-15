// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static ulong ExtractUInt64(this string @this)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < @this.Length; i++)
            {
                if (char.IsDigit(@this[i]))
                {
                    sb.Append(@this[i]);
                }
            }

            return Convert.ToUInt64(sb.ToString());
        }
    }
}
