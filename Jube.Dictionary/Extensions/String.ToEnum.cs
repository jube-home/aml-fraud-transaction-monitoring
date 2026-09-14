// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static T ToEnum<T>(this string @this)
        {
            var enumType = typeof(T);
            return (T)Enum.Parse(enumType, @this);
        }
    }
}
