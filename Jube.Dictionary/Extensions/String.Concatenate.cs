// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Concatenates a collection of strings.")]
        public static string Concatenate(this IEnumerable<string> @this)
        {
            var sb = new StringBuilder();

            foreach (var s in @this)
            {
                sb.Append(s);
            }

            return sb.ToString();
        }

        [Description("Concatenates the text the function returns for each item.")]
        public static string Concatenate<T>(this IEnumerable<T> source,
            [Description("The function that gives the text for each item.")] Func<T, string> func)
        {
            var sb = new StringBuilder();

            foreach (var item in source)
            {
                sb.Append(func(item));
            }

            return sb.ToString();
        }
    }
}