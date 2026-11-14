// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the tangent of the specified angle.")]
        public static Double Tan(this Double a)
        {
            return Math.Tan(a);
        }
    }
}
