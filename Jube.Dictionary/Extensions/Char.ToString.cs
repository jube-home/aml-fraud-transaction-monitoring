// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Char = char;
    using String = string;

    public static partial class Extensions
    {
        public static String ToString(this Char c)
        {
            return char.ToString(c);
        }
    }
}
