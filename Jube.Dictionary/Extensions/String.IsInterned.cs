// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        [Description("Retrieves a reference to the string from the intern pool.")]
        public static String? IsInterned(this String str)
        {
            return string.IsInterned(str);
        }
    }
}
