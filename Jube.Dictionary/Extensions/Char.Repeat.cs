// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Repeats the character a specified number of times.")]
        public static string Repeat(this char @this, [Description("Number of repeats.")] int repeatCount)
        {
            if (repeatCount > MaxRepeatLength)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatCount),
                    $"The repeated text is limited to {MaxRepeatLength} characters.");
            }

            return new string(@this, repeatCount);
        }
    }
}
