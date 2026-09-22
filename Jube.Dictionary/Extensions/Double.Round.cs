// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Int32 = int;
    using Math = Math;

    public static partial class Extensions
    {
        extension(Double a)
        {
            public Double Round()
            {
                return Math.Round(a);
            }

            public Double Round(Int32 digits)
            {
                return Math.Round(a, digits);
            }

            public Double Round(MidpointRounding mode)
            {
                return Math.Round(a, mode);
            }

            public Double Round(Int32 digits, MidpointRounding mode)
            {
                return Math.Round(a, digits, mode);
            }
        }
    }
}
