// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using Double = double;

    public static partial class Extensions
    {
        public static Boolean IsNaN(this Double d)
        {
            return double.IsNaN(d);
        }
    }
}
