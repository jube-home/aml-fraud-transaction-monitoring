// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;

    public static partial class Extensions
    {
        public static Double ToMoney(this Double @this)
        {
            return Math.Round(@this, 2);
        }
    }
}
