// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            public bool Contains(string value)
            {
                return @this.IndexOf(value, StringComparison.Ordinal) != -1;
            }

            public bool Contains(string value, StringComparison comparisonType)
            {
                return @this.IndexOf(value, comparisonType) != -1;
            }
        }
    }
}
