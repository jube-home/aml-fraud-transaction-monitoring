// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            public string ReplaceLast(string oldValue, string newValue)
            {
                var startindex = @this.LastIndexOf(oldValue, StringComparison.Ordinal);

                if (startindex == -1)
                {
                    return @this;
                }

                return @this.Remove(startindex, oldValue.Length).Insert(startindex, newValue);
            }

            public string ReplaceLast(int number, string oldValue, string newValue)
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
}
