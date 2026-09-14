// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        public static String Join(this String separator, String[] value)
        {
            return string.Join(separator, value);
        }

        public static String Join(this String separator, object[] values)
        {
            return string.Join(separator, values);
        }

        public static String Join<T>(this String separator, IEnumerable<T> values)
        {
            return string.Join(separator, values);
        }

        public static String Join(this String separator, IEnumerable<String> values)
        {
            return string.Join(separator, values);
        }

        public static String Join(this String separator, String[] value, Int32 startIndex, Int32 count)
        {
            return string.Join(separator, value, startIndex, count);
        }
    }
}
