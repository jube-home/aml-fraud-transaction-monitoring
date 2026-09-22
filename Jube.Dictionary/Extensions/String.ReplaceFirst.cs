// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            public string ReplaceFirst(string oldValue, string newValue)
            {
                var startindex = @this.IndexOf(oldValue, StringComparison.Ordinal);

                if (startindex == -1)
                {
                    return @this;
                }

                return @this.Remove(startindex, oldValue.Length).Insert(startindex, newValue);
            }

            public string ReplaceFirst(int number, string oldValue, string newValue)
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
