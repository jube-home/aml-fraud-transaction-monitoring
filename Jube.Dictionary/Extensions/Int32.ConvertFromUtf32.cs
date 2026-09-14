// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        public static String ConvertFromUtf32(this Int32 utf32)
        {
            return char.ConvertFromUtf32(utf32);
        }
    }
}
