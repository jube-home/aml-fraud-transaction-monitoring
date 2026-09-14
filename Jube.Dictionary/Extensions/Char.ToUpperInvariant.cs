// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Char = char;

    public static partial class Extensions
    {
        public static Char ToUpperInvariant(this Char c)
        {
            return char.ToUpperInvariant(c);
        }
    }
}
