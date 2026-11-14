// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using Math = Math;

    public static partial class Extensions
    {
        [Description("Returns the absolute value.")]
        public static Int32 Abs(this Int32 value)
        {
            return Math.Abs(value);
        }
    }
}
