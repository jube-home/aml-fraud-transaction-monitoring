// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using Char = char;

    public static partial class Extensions
    {
        public static Boolean IsUpper(this Char c)
        {
            return char.IsUpper(c);
        }
    }
}
