// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using Char = char;

    public static partial class Extensions
    {
        [Description("Checks if the character is a letter or decimal digit.")]
        public static Boolean IsLetterOrDigit(this Char c)
        {
            return char.IsLetterOrDigit(c);
        }
    }
}
