// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Produces the full product of two numbers.")]
        public static long BigMul(this Int32 a, [Description("The second number to multiply.")] Int32 b)
        {
            return Math.BigMul(a, b);
        }
    }
}
