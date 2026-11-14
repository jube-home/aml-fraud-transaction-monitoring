// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            [Description("Checks if the string contains all specified substrings.")]
            public bool ContainsAll([Description("The substrings to search for.")] params string[] values)
            {
                foreach (var value in values)
                {
                    if (@this.IndexOf(value, StringComparison.Ordinal) == -1)
                    {
                        return false;
                    }
                }

                return true;
            }

            [Description("Checks if the string contains all substrings with comparison rules.")]
            public bool ContainsAll([Description("The type of comparison to use.")] StringComparison comparisonType,
                [Description("The substrings to search for.")] params string[] values)
            {
                foreach (var value in values)
                {
                    if (@this.IndexOf(value, comparisonType) == -1)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
