// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using Math = Math;

    public static partial class Extensions
    {
        extension(Double d)
        {
            [Description("Returns the natural logarithm.")]
            public Double Log()
            {
                return Math.Log(d);
            }

            [Description("Returns the logarithm in a specified base.")]
            public Double Log([Description("The base of the logarithm.")] Double newBase)
            {
                return Math.Log(d, newBase);
            }
        }
    }
}
