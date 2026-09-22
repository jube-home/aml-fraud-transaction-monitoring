// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            public bool ContainsAny(params string[] values)
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

            public bool ContainsAny(StringComparison comparisonType, params string[] values)
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
