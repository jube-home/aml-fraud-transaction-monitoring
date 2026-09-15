// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        public static Int32 ConvertToUtf32(this String s, Int32 index)
        {
            return char.ConvertToUtf32(s, index);
        }
    }
}
