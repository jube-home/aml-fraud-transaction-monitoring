// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        extension(String separator)
        {
            public String Join(String[] value)
            {
                return string.Join(separator, value);
            }

            public String Join(object[] values)
            {
                return string.Join(separator, values);
            }

            public String Join<T>(IEnumerable<T> values)
            {
                return string.Join(separator, values);
            }

            public String Join(IEnumerable<String> values)
            {
                return string.Join(separator, values);
            }

            public String Join(String[] value, Int32 startIndex, Int32 count)
            {
                return string.Join(separator, value, startIndex, count);
            }
        }
    }
}
