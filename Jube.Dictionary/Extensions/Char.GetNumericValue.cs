// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Char = char;
    using Double = double;

    public static partial class Extensions
    {
        public static Double GetNumericValue(this Char c)
        {
            return char.GetNumericValue(c);
        }
    }
}
