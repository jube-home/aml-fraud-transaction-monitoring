// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the smallest integral value greater than or equal to the number.")]
        public static Double Ceiling(this Double a)
        {
            return Math.Ceiling(a);
        }
    }
}
