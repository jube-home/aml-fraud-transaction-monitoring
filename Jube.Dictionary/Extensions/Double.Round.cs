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
            [Description("Rounds to the nearest integral value.")]
            public Double Round()
            {
                return Math.Round(a);
            }

            [Description("Rounds to a specified number of fractional digits.")]
            public Double Round([Description("The number of fractional digits in the return value.")] Int32 digits)
            {
                return Math.Round(a, digits);
            }

            [Description("Rounds using specified rounding rules.")]
            public Double Round([Description("Specification for how to round if it is midway between two other numbers.")] MidpointRounding mode)
            {
                return Math.Round(a, mode);
            }

            [Description("Rounds to specified digits using rounding rules.")]
            public Double Round([Description("The number of fractional digits in the return value.")] Int32 digits,
                [Description("Specification for how to round if it is midway between two other numbers.")] MidpointRounding mode)
            {
                return Math.Round(a, digits, mode);
            }
        }
    }
}
