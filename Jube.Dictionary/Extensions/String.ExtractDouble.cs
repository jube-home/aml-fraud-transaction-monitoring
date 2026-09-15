// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static double ExtractDouble(this string @this)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < @this.Length; i++)
            {
                if (char.IsDigit(@this[i]) || @this[i] == '.')
                {
                    if (sb.Length == 0 && i > 0 && @this[i - 1] == '-')
                    {
                        sb.Append('-');
                    }

                    sb.Append(@this[i]);
                }
            }

            return Convert.ToDouble(sb.ToString());
        }
    }
}
