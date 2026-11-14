// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            [Description("Replaces the last occurrence of a substring.")]
            public string ReplaceLast([Description("The value to replace.")] string oldValue,
                [Description("The replacement value.")] string newValue)
            {
                var startindex = @this.LastIndexOf(oldValue, StringComparison.Ordinal);

                if (startindex == -1)
                {
                    return @this;
                }

                return @this.Remove(startindex, oldValue.Length).Insert(startindex, newValue);
            }

            [Description("Replaces the last N occurrences of a substring.")]
            public string ReplaceLast([Description("The number of occurrences to replace.")] int number,
                [Description("The value to replace.")] string oldValue,
                [Description("The replacement value.")] string newValue)
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
