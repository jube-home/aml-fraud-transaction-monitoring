// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            [Description("Replaces the first occurrence of a substring.")]
            public string ReplaceFirst([Description("The value to replace.")] string oldValue,
                [Description("The replacement value.")] string newValue)
            {
                var startindex = @this.IndexOf(oldValue, StringComparison.Ordinal);

                if (startindex == -1)
                {
                    return @this;
                }

                return @this.Remove(startindex, oldValue.Length).Insert(startindex, newValue);
            }

            [Description("Replaces the first N occurrences of a substring.")]
            public string ReplaceFirst([Description("The number of occurrences to replace.")] int number,
                [Description("The value to replace.")] string oldValue,
                [Description("The replacement value.")] string newValue)
            {
                var list = @this.Split(oldValue).ToList();
                var old = number + 1;
                var listStart = list.Take(old);
                var listEnd = list.Skip(old).ToList();

                return string.Join(newValue, listStart) +
                       (listEnd.Count > 0 ? oldValue : "") +
                       string.Join(oldValue, listEnd);
            }
        }
    }
}
