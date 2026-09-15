// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        public static bool IsOdd(this Int32 @this)
        {
            return @this % 2 != 0;
        }
    }
}
