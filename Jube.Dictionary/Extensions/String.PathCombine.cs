// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string PathCombine(this string @this, params string[] paths)
        {
            var list = paths.ToList();
            list.Insert(0, @this);
            return Path.Combine(list.ToArray());
        }
    }
}
