// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Char = char;

    public static partial class Extensions
    {
        [Description("Converts the character to lowercase using invariant culture rules.")]
        public static Char ToLowerInvariant(this Char c)
        {
            return char.ToLowerInvariant(c);
        }
    }
}
