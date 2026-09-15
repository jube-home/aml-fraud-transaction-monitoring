// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        public static bool FactorOf(this Int32 @this, Int32 factorNumer)
        {
            return factorNumer % @this == 0;
        }
    }
}
