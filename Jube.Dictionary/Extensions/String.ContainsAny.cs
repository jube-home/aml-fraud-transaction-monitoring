// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            [Description("Checks if the string contains any of the specified substrings.")]
            public bool ContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                foreach (var value in values)
                {
                    if (@this.IndexOf(value, StringComparison.Ordinal) != -1)
                    {
                        return true;
                    }
                }

                return false;
            }

            [Description("Checks if the string contains any substrings with comparison rules.")]
            public bool ContainsAny([Description("The type of comparison to use.")] StringComparison comparisonType,
                [Description("The substrings to search for.")] params string[] values)
            {
                foreach (var value in values)
                {
                    if (@this.IndexOf(value, comparisonType) != -1)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
