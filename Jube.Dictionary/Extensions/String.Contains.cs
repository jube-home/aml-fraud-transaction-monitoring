// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            [Description("Checks if the string contains a substring.")]
            public bool Contains([Description("The substring to search for.")] string value)
            {
                return @this.IndexOf(value, StringComparison.Ordinal) != -1;
            }

            [Description("Checks if the string contains a substring with comparison rules.")]
            public bool Contains([Description("The substring to search for.")] string value,
                [Description("The type of comparison to use.")] StringComparison comparisonType)
            {
                return @this.IndexOf(value, comparisonType) != -1;
            }
        }
    }
}
