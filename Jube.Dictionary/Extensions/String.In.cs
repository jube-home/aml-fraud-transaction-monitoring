// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        public static bool In(this String @this, params String[] values)
        {
            return Array.IndexOf(values, @this) != -1;
        }
    }
}
