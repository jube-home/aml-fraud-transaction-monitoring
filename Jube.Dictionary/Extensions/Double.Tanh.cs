// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the hyperbolic tangent of the specified angle.")]
        public static Double Tanh(this Double value)
        {
            return Math.Tanh(value);
        }
    }
}
