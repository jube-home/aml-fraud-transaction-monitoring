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
            [Description("Joins an array of strings with a separator.")]
            public String Join([Description("An array that contains the elements to concatenate.")] String[] value)
            {
                return string.Join(separator, value);
            }

            [Description("Joins a string collection with a separator.")]
            public String Join([Description("An array that contains the elements to concatenate.")] object[] values)
            {
                return string.Join(separator, values);
            }

            [Description("Joins a string collection with a separator.")]
            public String Join<T>([Description("An array that contains the elements to concatenate.")] IEnumerable<T> values)
            {
                return string.Join(separator, values);
            }

            [Description("Joins a string collection with a separator.")]
            public String Join([Description("An array that contains the elements to concatenate.")] IEnumerable<String> values)
            {
                return string.Join(separator, values);
            }

            [Description("Joins a subset of an array with a separator.")]
            public String Join([Description("An array that contains the elements to concatenate.")] String[] value,
                [Description("The first element in value to use.")] Int32 startIndex,
                [Description("The number of elements of value to use.")] Int32 count)
            {
                return string.Join(separator, value, startIndex, count);
            }
        }
    }
}
