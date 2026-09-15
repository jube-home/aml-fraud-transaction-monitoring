// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using Int32 = int;
    using String = string;

    public static partial class Extensions
    {
        public static Boolean IsDigit(this String s, Int32 index)
        {
            return char.IsDigit(s, index);
        }
    }
}
