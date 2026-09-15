// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string Extract(this string @this, Func<char, bool> predicate)
        {
            return new string(@this.ToCharArray().Where(predicate).ToArray());
        }
    }
}
