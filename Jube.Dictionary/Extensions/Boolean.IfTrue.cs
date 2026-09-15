// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static void IfTrue(this bool @this, Action action)
        {
            if (@this)
            {
                action();
            }
        }
    }
}
