// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string ReplaceLast(this string @this, string oldValue, string newValue)
        {
            var startindex = @this.LastIndexOf(oldValue, StringComparison.Ordinal);

            if (startindex == -1)
            {
                return @this;
            }

            return @this.Remove(startindex, oldValue.Length).Insert(startindex, newValue);
        }

        public static string ReplaceLast(this string @this, int number, string oldValue, string newValue)
        {
            var list = @this.Split(oldValue).ToList();
            var old = Math.Max(0, list.Count - number - 1);
            var listStart = list.Take(old);
            var listEnd = list.Skip(old);

            return string.Join(oldValue, listStart) +
                   (old > 0 ? oldValue : "") +
                   string.Join(newValue, listEnd);
        }
    }
}
