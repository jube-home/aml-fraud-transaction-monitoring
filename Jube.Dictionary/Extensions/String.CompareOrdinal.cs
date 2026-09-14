// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        public static Int32 CompareOrdinal(this String strA, String strB)
        {
            return string.CompareOrdinal(strA, strB);
        }

        public static Int32 CompareOrdinal(this String strA, Int32 indexA, String strB, Int32 indexB, Int32 length)
        {
            return string.CompareOrdinal(strA, indexA, strB, indexB, length);
        }
    }
}
