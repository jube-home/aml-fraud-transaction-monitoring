// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        public static bool IsPrime(this Int32 @this)
        {
            if (@this < 2)
            {
                return false;
            }

            if (@this == 2)
            {
                return true;
            }

            if (@this % 2 == 0)
            {
                return false;
            }

            var sqrt = (Int32)Math.Sqrt(@this);
            for (var t = 3; t <= sqrt; t += 2)
            {
                if (@this % t == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
