// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        extension(String str0)
        {
            [Description("Concatenates two strings.")]
            public String Concat([Description("The second string to concatenate.")] String str1)
            {
                return string.Concat(str0, str1);
            }

            [Description("Concatenates three strings.")]
            public String Concat([Description("The second string to concatenate.")] String str1,
                [Description("The third string to concatenate.")] String str2)
            {
                return string.Concat(str0, str1, str2);
            }

            [Description("Concatenates four strings.")]
            public String Concat([Description("The second string to concatenate.")] String str1,
                [Description("The third string to concatenate.")] String str2,
                [Description("The fourth string to concatenate.")] String str3)
            {
                return string.Concat(str0, str1, str2, str3);
            }
        }
    }
}
