// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        internal const int MaxRepeatLength = 100000;

        [Description("Repeats the string a specified number of times.")]
        public static string Repeat(this string @this, [Description("Number of repeats.")] int repeatCount)
        {
            if (repeatCount > 0 && (long)repeatCount * @this.Length > MaxRepeatLength)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatCount),
                    $"The repeated text is limited to {MaxRepeatLength} characters.");
            }

            if (@this.Length == 1)
            {
                return new string(@this[0], repeatCount);
            }

            var sb = new StringBuilder(repeatCount * @this.Length);

            while (repeatCount-- > 0)
            {
                sb.Append(@this);
            }

            return sb.ToString();
        }
    }
}