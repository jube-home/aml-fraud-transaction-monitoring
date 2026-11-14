// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Enumerates from the current character to the specified character.")]
        public static IEnumerable<char> To(this char @this, [Description("The target character.")] char toCharacter)
        {
            var reverseRequired = @this > toCharacter;

            var first = reverseRequired ? toCharacter : @this;
            var last = reverseRequired ? @this : toCharacter;

            var result = Enumerable.Range(first, last - first + 1).Select(charCode => (char)charCode);

            if (reverseRequired)
            {
                result = result.Reverse();
            }

            return result;
        }
    }
}
