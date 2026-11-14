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
            [Description("Compares two strings using ordinal rules.")]
            public Int32 CompareOrdinal([Description("The second string to compare.")] String strB)
            {
                return string.CompareOrdinal(strA, strB);
            }

            [Description("Compares substrings using ordinal rules.")]
            public Int32 CompareOrdinal([Description("The starting index of the substring in strA.")] Int32 indexA,
                [Description("The second string to use in the comparison.")] String strB,
                [Description("The starting index of the substring in strB.")] Int32 indexB,
                [Description("The maximum number of characters in the substrings to compare.")] Int32 length)
            {
                return string.CompareOrdinal(strA, indexA, strB, indexB, length);
            }
        }
    }
}
