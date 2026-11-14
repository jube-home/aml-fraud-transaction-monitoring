// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the largest integer less than or equal to the number.")]
        public static Double Floor(this Double d)
        {
            return Math.Floor(d);
        }
    }
}
