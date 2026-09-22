// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        extension(String str0)
        {
            public String Concat(String str1)
            {
                return string.Concat(str0, str1);
            }

            public String Concat(String str1, String str2)
            {
                return string.Concat(str0, str1, str2);
            }

            public String Concat(String str1, String str2, String str3)
            {
                return string.Concat(str0, str1, str2, str3);
            }
        }
    }
}
