// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the angle whose tangent is the specified number.")]
        public static Double Atan(this Double d)
        {
            return Math.Atan(d);
        }
    }
}
