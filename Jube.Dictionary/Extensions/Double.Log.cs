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
            public Double Log()
            {
                return Math.Log(d);
            }

            public Double Log(Double newBase)
            {
                return Math.Log(d, newBase);
            }
        }
    }
}
