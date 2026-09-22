// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        extension(String strA)
        {
            public Int32 CompareOrdinal(String strB)
            {
                return string.CompareOrdinal(strA, strB);
            }

            public Int32 CompareOrdinal(Int32 indexA, String strB, Int32 indexB, Int32 length)
            {
                return string.CompareOrdinal(strA, indexA, strB, indexB, length);
            }
        }
    }
}
