// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the angle whose cosine is the specified number.")]
        public static Double Acos(this Double d)
        {
            return Math.Acos(d);
        }
    }
}
