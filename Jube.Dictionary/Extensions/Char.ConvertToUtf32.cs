// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Char = char;
    using Int32 = int;

    public static partial class Extensions
    {
        public static Int32 ConvertToUtf32(this Char highSurrogate, Char lowSurrogate)
        {
            return char.ConvertToUtf32(highSurrogate, lowSurrogate);
        }
    }
}
